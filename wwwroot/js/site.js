// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function aplicarFiltroOnClick() {
    var selectedCategories = $("input[name='categories']:checked").map(function () {
        return $(this).val();
    }).get();
    var selectedSizes = $("input[name='sizes']:checked").map(function () {
        return $(this).val();
    }).get();

    $.ajax({
        url: "/Home/AplicarFiltro",
        type: "GET",
        data: {
            categories: selectedCategories.join(','),
            sizes: selectedSizes.join(',')
        },
        success: function (result) {
            $("#ListingIndexId").html(result);
        },
        error: function (error) {
            console.error("Error:", error);
        }

    });
}
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
