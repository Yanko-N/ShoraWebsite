// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


function clothsOnClick(size) {
    var checkBox = document.getElementById(size);
    var textInput = document.getElementById("ID_" + size);

    console.log(size);
    if (checkBox.checked) {
        textInput.disabled = false;
    } else {
        textInput.disabled = true;
    }
}

function PopUpConfirmation(message) {
    return confirm(message);
}
function closePopUpMessage(id) {
    var div = document.getElementById(id);

    div.remove();
}
