# SignaturesDemo

Demo educativa de firma electrónica de PDFs con **AutoFirma** + **Cliente Móvil @firma**
sobre **.NET 8** (ASP.NET Core API + Blazor Server).

El usuario abre la web, el servidor le entrega un PDF generado al vuelo, lo firma con
su certificado digital local (FNMT, DNIe, etc.) a través de AutoFirma, y el servidor
valida la firma **PAdES** resultante.

> Esta demo está orientada al aprendizaje. Sirve para entender el flujo end-to-end de
> firma local; no es una integración lista para producción comercial (ver sección
> *Licencias y consideraciones*).

---

## Por qué este enfoque

Los navegadores **ya no acceden** al almacén de certificados desde JavaScript: NPAPI
(Chrome) y Java applets (Firefox) se eliminaron entre 2015 y 2017. Hoy, las webs que
piden firmas con certificados de usuario delegan en una **aplicación nativa** invocada
por *protocol handler*:

```
[Web]  ──(afirma://...)──▶  [AutoFirma local]  ──▶  [Almacén del SO / DNIe]
                                                       │
[Web]  ◀────  PDF firmado en PAdES ──────────────────┘
```

AutoFirma es la app de referencia del Gobierno español (Ministerio de Asuntos
Económicos y Transformación Digital). Es gratuita, multiplataforma (Win / macOS /
Linux) y tiene equivalente móvil en iOS y Android. La integración web se hace con
`autoscript.js`, el cliente JS oficial del proyecto
[ctt-gob-es/clienteafirma](https://github.com/ctt-gob-es/clienteafirma).

## Estructura del repo

```
signaturesDemo/
├── global.json                # fija SDK .NET 8
├── SignaturesDemo.sln
└── src/
    ├── SignaturesDemo.Api/    # ASP.NET Core Web API (.NET 8)
    │   ├── Controllers/
    │   │   ├── PdfController.cs          # GET /api/pdf/sample
    │   │   └── SignaturesController.cs   # POST /api/signatures/validate
    │   ├── Services/
    │   │   ├── SamplePdfGenerator.cs     # QuestPDF
    │   │   └── SignatureValidator.cs     # iText 7
    │   └── Program.cs                    # CORS, Swagger, DI
    └── SignaturesDemo.Web/    # Blazor Web App (.NET 8, modo Server)
        ├── Components/
        │   ├── App.razor                 # carga autoscript.js
        │   ├── Layout/
        │   └── Pages/
        │       ├── Home.razor            # landing
        │       └── Firmar.razor          # flujo de firma
        └── wwwroot/
            ├── js/autofirma-integration.js  # wrapper Promise-based
            └── lib/afirma/autoscript.js     # cliente oficial @firma
```

## Requisitos

| Componente               | Versión / Notas                                                    |
| ------------------------ | ------------------------------------------------------------------ |
| .NET SDK                 | 8.0.4xx (fijado en `global.json`)                                  |
| AutoFirma (escritorio)   | ≥ 1.8, descarga oficial: https://firmaelectronica.gob.es           |
| Cliente Móvil @firma     | Play Store / App Store (sólo si vas a probar desde móvil)          |
| Certificado digital      | FNMT, DNIe electrónico, o cualquier cert software/tarjeta soportado |

> En Windows, comprueba que tu certificado FNMT aparece en `certmgr` →
> *Personal* → *Certificados* (es donde AutoFirma lo busca por defecto).

## Cómo ejecutar la demo

```powershell
# desde la raíz del repo
dotnet build

# terminal 1: API en https://localhost:7001
dotnet run --project src/SignaturesDemo.Api/SignaturesDemo.Api.csproj

# terminal 2: Web en https://localhost:7002
dotnet run --project src/SignaturesDemo.Web/SignaturesDemo.Web.csproj
```

Abre `https://localhost:7002/firmar`. La página:

1. Pide un PDF de prueba al API y lo muestra embebido.
2. Al pulsar **Firmar**, dispara AutoFirma.
3. Recibe el PDF firmado y lo manda al API para validarlo.
4. Muestra el resultado: validez, CN del firmante, CA emisora, algoritmo, fechas, etc.

### Probar desde móvil

El navegador móvil necesita poder alcanzar la web. Como por defecto Kestrel solo escucha
en `localhost`, tienes que exponer el puerto al dispositivo. Lo más simple:

```powershell
# Cambia 0.0.0.0 (todas las interfaces) por la IP de tu PC en la LAN
$env:ASPNETCORE_URLS = "https://0.0.0.0:7002"
dotnet run --project src/SignaturesDemo.Web/SignaturesDemo.Web.csproj
```

Acepta el certificado autofirmado de desarrollo en el móvil (será necesario instalarlo)
y abre `https://<tu-ip-LAN>:7002/firmar`. Al pulsar **Firmar**, el navegador móvil
debería abrir la app *Cliente Móvil @firma* / *AutoFirma* mediante un *deep link*.

> Limitación conocida: en algunos navegadores Android no se puede abrir el deep link
> directamente desde un sitio HTTPS con certificado no confiado. Para una prueba
> rápida, expón también HTTP en LAN (sólo para entornos de desarrollo).

## Flujo técnico, paso a paso

```
┌─────────────────┐   GET /api/pdf/sample    ┌─────────────────────┐
│  Blazor Server  │ ───────────────────────▶ │  SignaturesDemo.Api │
│  (página /firmar)│                          │  (QuestPDF)         │
│                 │ ◀─── PDF (bytes) ──────  │                     │
└────────┬────────┘                          └─────────────────────┘
         │ window.SignaturesDemo.signPdf(b64)
         ▼
┌─────────────────┐                          ┌─────────────────────┐
│ autoscript.js    │ ──── afirma://... ────▶ │  AutoFirma (local)  │
│ (cliente @firma) │                          │  - selecciona cert  │
│                 │ ◀───  firma base64 ───── │  - pide PIN         │
└────────┬────────┘                          │  - aplica RSA-SHA256│
         │                                   │  - genera PAdES     │
         │  POST /api/signatures/validate   └─────────────────────┘
         ▼
┌─────────────────────┐
│  SignaturesDemo.Api │ ── iText 7 lee firmas, integridad, cert ──▶ JSON con datos
└─────────────────────┘
```

## API endpoints

### `GET /api/pdf/sample`

Devuelve un PDF nuevo (Content-Type `application/pdf`). Cada llamada genera un
documento con un identificador único.

### `POST /api/signatures/validate`

```http
POST /api/signatures/validate
Content-Type: application/json

{ "signedPdfBase64": "JVBERi0xLjcK..." }
```

Respuesta:

```json
{
  "isValid": true,
  "signatures": [
    {
      "fieldName": "Signature1",
      "integrityValid": true,
      "coversWholeDocument": true,
      "signerCommonName": "VILLAR GURRUCHAGA YAGO - 09089002T",
      "signerSubject": "CN=VILLAR GURRUCHAGA YAGO ...",
      "issuerCommonName": "AC FNMT Usuarios",
      "algorithm": "SHA256withRSA",
      "signedAt": "2026-05-12T13:24:11Z",
      "certificateNotBefore": "2025-01-17T...",
      "certificateNotAfter": "2029-01-17T...",
      "certificateSerialHex": "..."
    }
  ]
}
```

## Resolución de problemas

| Síntoma                                              | Causa probable                                                          | Solución                                                                              |
| ---------------------------------------------------- | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| "No se pudo obtener el PDF del API"                  | El API no está corriendo                                                | Arranca `SignaturesDemo.Api` en otra terminal                                         |
| `CORS error` en consola del navegador                | URL de la Web no incluida en la política CORS del API                   | Edita `Program.cs` del API → `WebCorsPolicy.WithOrigins(...)`                         |
| "El cliente @firma no se ha cargado"                 | `autoscript.js` no se sirvió correctamente                              | Comprueba que existe `wwwroot/lib/afirma/autoscript.js` y que `App.razor` lo referencia |
| El botón Firmar abre AutoFirma pero da error SAF_xx  | Versión de AutoFirma incompatible con autoscript.js                     | Actualiza AutoFirma a la última versión estable                                       |
| AutoFirma no se abre en absoluto                     | Protocol handler `afirma://` no registrado                              | Reinstala AutoFirma; en Linux puede requerir registro manual con `xdg-mime`           |
| "Firma no válida" en validación                      | El PDF se modificó tras firmar                                          | Reinicia el flujo desde el paso 1                                                     |
| Móvil: el deep link no abre AutoFirma                | App móvil no instalada, o navegador bloquea deep links desde HTTPS auto | Instala app, prueba con red local en HTTP o cert confiable                            |

## Licencias y consideraciones

- **iText 7** se distribuye bajo **AGPL**. Vale para demos y proyectos AGPL/open
  source, pero si SignaturesDemo evolucionara a un producto comercial cerrado,
  habría que comprar licencia comercial o migrar a un stack alternativo
  (p. ej. BouncyCastle puro).
- **QuestPDF Community** es MIT.
- **autoscript.js / Cliente @firma** se distribuye bajo GPL 2+ y EUPL 1.1. La copia
  incluida procede de la versión oficial publicada en
  [ctt-gob-es/clienteafirma](https://github.com/ctt-gob-es/clienteafirma).
- **Cl@ve Firma** (firma centralizada en HSM de la FNMT) **no es integrable** por
  entidades privadas: es un servicio reservado al Sector Público Administrativo y
  requiere convenio con la SGAD. Equivalentes para empresa privada: FNMT "Firma en
  la Nube", Viafirma, Signaturit, Validated ID, Uanataca, etc.

## Comparativa de modelos de firma

Para entender en qué encaja AutoFirma frente a Cl@ve Firma y a un TSP comercial,
con diagramas de secuencia y la lista de requisitos como empresa/organismo, ver
[docs/MODELOS-FIRMA.md](docs/MODELOS-FIRMA.md).

## Recursos

- Cliente @firma (repo oficial): https://github.com/ctt-gob-es/clienteafirma
- Wiki de integración: https://github.com/ctt-gob-es/clienteafirma/wiki
- Especificación PAdES: ETSI EN 319 142-1
- Reglamento eIDAS (UE) 910/2014
- Portal FNMT: https://www.fnmt.es/ceres
