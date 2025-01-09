window.adjustTextareaHeight = function (textareaElement) {
    textareaElement.style.height = 'auto';  // сбрасываем высоту, чтобы она подстроилась под контент
    textareaElement.style.height = (textareaElement.scrollHeight) + 'px';  // устанавливаем высоту в зависимости от содержимого
}