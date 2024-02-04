// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function SearchClothsOnClick() {
    var input = document.getElementById("searchInputId");
    var inputValue = input.value;
    $.ajax({
        url: "/Home/Search",
        type: "GET",
        data: {
            search: inputValue
        },
        success: function (result) {
            input.value = "";
            $("#ListingIndexId").html(result);
        },
        error: function (error) {
            console.error("Error:", error);
        }

    });
}
function RemoverFiltroOnClick() {
    // Uncheck 
    $("input[name='categories']").prop('checked', false);

    // Uncheck
    $("input[name='sizes']").prop('checked', false);

    var input = document.getElementById("searchInputId");


    $.ajax({
        url: "/Home/RemoverFiltro",
        type: "GET",
        success: function (result) {
            $("#ListingIndexId").html(result);
            input.value = "";

        },
        error: function (error) {
            console.error("Error:", error);
        }

    });
}
function AplicarFiltroOnClick() {

    var input = document.getElementById("searchInputId");
    var inputValue = input.value;

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
            search : inputValue,
            categories: selectedCategories.join(','),
            sizes: selectedSizes.join(',')
        },
        success: function (result) {
            $("#ListingIndexId").html(result);
            input.value = "";
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
