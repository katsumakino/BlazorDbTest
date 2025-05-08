async function captureScreenshot() {
	const canvas = await html2canvas(document.body);
	return canvas.toDataURL("image/png");
}

function filterForbiddenWords(input) {
  // 禁止文字列のリスト
  const forbiddenWords = ["\"", ",", "\'"];

  // 入力値をチェックし、禁止文字列を削除
  forbiddenWords.forEach(word => {
    const regex = new RegExp(word, 'g'); // 禁止文字列を正規表現で検索
    input.value = input.value.replace(regex, ''); // 禁止文字列を削除
  });
}

document.addEventListener("DOMContentLoaded", () => {
  const inputElement = document.getElementById("textInput");

  inputElement.addEventListener("keydown", (event) => {
    // 入力をキャンセルしたい文字を指定（例: "@" を禁止）
    if (event.key === "@") {
      event.preventDefault(); // 入力をキャンセル
    }
  });
});
