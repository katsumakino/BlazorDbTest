async function captureScreenshot() {
	const canvas = await html2canvas(document.body);
	return canvas.toDataURL("image/png");
}
