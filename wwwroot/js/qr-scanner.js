window.kerRandoQrScanner = (() => {
    const scanners = new Map();

    return {
        async start(elementId, dotNetReference) {
            if (!window.Html5Qrcode) {
                throw new Error("The QR scanner library is unavailable.");
            }

            const scanner = new window.Html5Qrcode(elementId);
            let handled = false;
            scanners.set(elementId, scanner);

            try {
                await scanner.start(
                    { facingMode: "environment" },
                    { fps: 10, qrbox: 250, aspectRatio: 1 },
                    async decodedText => {
                        if (handled) return;
                        handled = true;
                        try {
                            await scanner.stop();
                        } catch {
                        }
                        scanners.delete(elementId);
                        await dotNetReference.invokeMethodAsync("OnQrDecoded", decodedText);
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