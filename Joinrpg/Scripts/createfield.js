var cbFieldType;
var flPrice;

function fieldTypeChanged()
{
    // TODO: Generate this function using ProjectFieldViewType helper
    switch (parseInt(cbFieldType.value))
    {
        //case 2:
        case 3:
        //case 4:
        case 6:
            flPrice.style.display = "block";
            break;
        default:
            flPrice.style.display = "none";
    }
}

$(function ()
{
    cbFieldType = document.getElementById("FieldViewType");
    cbFieldType.addEventListener("change", fieldTypeChanged);
    flPrice = document.getElementById("priceBlock");
});
