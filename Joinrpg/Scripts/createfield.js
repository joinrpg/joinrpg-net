$(function ()
{
    var flPrice = document.getElementById("priceBlock");
    var flValidForNpc = document.getElementById("validForNpcBlock");
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
    var cbBoundTo = document.getElementById("FieldBoundTo");
    cbBoundTo.addEventListener("change", function ()
    {
        if (cbBoundTo && flValidForNpc)
            switch (parseInt(cbBoundTo.value))
            {
                case 0:
                    flValidForNpc.style.display = "block";
                    break;
                case 1:
                    flValidForNpc.style.display = "none";
                    break;
            }
    });
});
