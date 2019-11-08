$(function ()
{
    var flPrice = document.getElementById("priceBlock");
    var cbFieldType = document.getElementById("FieldViewType");
    cbFieldType.addEventListener("change", function ()
    {
        // TODO: Generate this function using ProjectFieldViewType helper
        if (cbFieldType && flPrice)
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
    });
});
