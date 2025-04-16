function openPdfInNewTab(url) {
	window.open(url, '_blank');
}

function openPdfInIframeAndPrint(url) {
  var iframe = document.getElementById('pdfFrame');
  iframe.src = url;
  iframe.onload = function () {
    iframe.contentWindow.print();
  };
}
