window.kerRandoQrScanner = (() => {
    const scanners = new Map();

    return {
        async start(elementId, dotNetReference) {
            if (!window.Html5Qrcode) {
                throw new Error("The QR scanner library is unavailable.");
            }

            const scanner = new window.Html5Qrcode(elementId);
            let handled = false;
            let lastDecodedText;
            let retryAfter = 0;
            scanners.set(elementId, scanner);

            try {
                await scanner.start(
                    { facingMode: "environment" },
                    { fps: 10, qrbox: 250, aspectRatio: 1 },
                    async decodedText => {
                        if (handled || (decodedText === lastDecodedText && Date.now() < retryAfter)) return;
                        handled = true;
                        lastDecodedText = decodedText;
                        let accepted = false;
                        try {
                            accepted = await dotNetReference.invokeMethodAsync("OnQrDecoded", decodedText);
                        } finally {
                            if (!accepted) {
                                retryAfter = Date.now() + 1500;
                                handled = false;
                            }
                        }
                    },
                    () => { }
                );
            } catch (error) {
                scanners.delete(elementId);
                try {
                    await scanner.clear();
                } catch {
                }
                throw error;
            }
        },

        async stop(elementId) {
            const scanner = scanners.get(elementId);
            if (!scanner) return;
            scanners.delete(elementId);
            try {
                await scanner.stop();
            } finally {
                await scanner.clear();
            }
        }
    };
})();