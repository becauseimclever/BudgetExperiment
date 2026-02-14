// File download helper for client exports.

export async function downloadFileFromStream(fileName, contentType, streamRef) {
    const arrayBuffer = await streamRef.arrayBuffer();
    const type = contentType || "application/octet-stream";
    const blob = new Blob([arrayBuffer], { type });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName || "export";
    anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}
