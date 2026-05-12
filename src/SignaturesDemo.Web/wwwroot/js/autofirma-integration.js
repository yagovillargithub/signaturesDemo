// Thin wrapper around the official Cliente @firma autoscript.js so that Blazor
// can call into it. autoscript.js exposes a global `AutoScript` object once
// loaded. We bridge its callback-based `sign` API into a Promise the .NET side
// can await.
//
// Flow:
//   1. ensureLoaded()      → returns autoscript version + UA info (for the log)
//   2. loadClient()        → calls AutoScript.cargarAppAfirma() so the client
//                            picks the correct transport (WebSocket / deep link
//                            / web-service) for the platform. Synchronous; no
//                            callbacks. Idempotent.
//   3. signPdf(b64, hint)  → opens AutoFirma (desktop) or the @firma app
//                            (mobile) via the underlying transport. Resolves
//                            with the signed PDF (base64). Rejects with
//                            {code, type, message} on error.
//
// Logs every step to console so the user can debug from DevTools (F12).
// autoscript.js itself never reaches into the OS keystore — it always delegates
// to AutoFirma, which talks to the OS keystore (Windows / macOS / Linux /
// Android / iOS).

(function (window) {
    "use strict";

    const log = function () {
        try { console.log.apply(console, ["[SignaturesDemo]"].concat([].slice.call(arguments))); } catch (_) { }
    };

    function ensureAutoScript() {
        if (typeof window.AutoScript === "undefined") {
            throw new Error(
                "El cliente @firma (autoscript.js) no se ha cargado. " +
                "Comprueba que wwwroot/lib/afirma/autoscript.js está accesible."
            );
        }
        return window.AutoScript;
    }

    let _clientLoaded = false;

    window.SignaturesDemo = {
        ensureLoaded: function () {
            const a = ensureAutoScript();
            const info = (a.VERSION || "(unknown)") + " | UA: " + navigator.userAgent;
            log("autoscript loaded:", info);
            return info;
        },

        // cargarAppAfirma is synchronous: it picks a transport (WebSocket /
        // socket / deep link / web-service) and stores it in a closure. There
        // are no success/error callbacks — failure is reported when sign()
        // tries to actually use the transport.
        loadClient: function () {
            const a = ensureAutoScript();
            if (_clientLoaded) {
                log("client already loaded, skipping");
                return true;
            }
            try {
                a.cargarAppAfirma();
                _clientLoaded = true;
                log("AutoScript.cargarAppAfirma() invoked");
                return true;
            } catch (e) {
                log("cargarAppAfirma threw:", e);
                throw e;
            }
        },

        // signPdf(pdfBase64, options)
        //   options.reason          → optional, free-text reason
        //   options.signatureField  → optional, name of an empty signature
        //                             widget already in the PDF; if present,
        //                             AutoFirma paints the visible signature
        //                             inside that widget's rectangle.
        signPdf: function (pdfBase64, options) {
            return new Promise(function (resolve, reject) {
                let a;
                try {
                    a = ensureAutoScript();
                } catch (e) {
                    reject({ type: "FATAL", message: e.message, code: "AUTOSCRIPT_MISSING" });
                    return;
                }

                if (!_clientLoaded) {
                    try { a.cargarAppAfirma(); _clientLoaded = true; }
                    catch (e) {
                        reject({ type: "FATAL", message: e.message, code: "LOAD_FAILED" });
                        return;
                    }
                }

                const opts = options || {};
                const reason = opts.reason || opts.Reason || "";
                const fieldName = opts.signatureField || opts.SignatureField || "";

                let extraParams =
                    "signatureProductionCity=Madrid\n" +
                    "signerContact=signaturesDemo@example.org\n";
                if (reason) extraParams += "signReason=" + reason + "\n";
                if (fieldName) extraParams += "signatureField=" + fieldName + "\n";

                log("extraParams:\n" + extraParams);

                log("calling AutoScript.sign() — this will open AutoFirma");
                log("pdf size (base64 chars):", pdfBase64.length);

                try {
                    a.sign(
                        pdfBase64,
                        "SHA256withRSA",
                        "PAdES",
                        extraParams,
                        function (signatureB64, certB64, extraData) {
                            log("sign success — signature length:", signatureB64 ? signatureB64.length : 0);
                            resolve({
                                signedPdfBase64: signatureB64,
                                certificateBase64: certB64 || null,
                                extra: extraData || null
                            });
                        },
                        function (type, message) {
                            const code = a.getErrorCode ? a.getErrorCode() : null;
                            log("sign error:", type, message, "code:", code);
                            reject({
                                type: type,
                                message: message,
                                code: code || "SIGN_FAILED"
                            });
                        }
                    );
                } catch (e) {
                    log("sign() threw synchronously:", e);
                    reject({ type: "FATAL", message: e.message, code: "SIGN_THREW" });
                }
            });
        }
    };

    log("autofirma-integration loaded, AutoScript present:", typeof window.AutoScript !== "undefined");
})(window);
