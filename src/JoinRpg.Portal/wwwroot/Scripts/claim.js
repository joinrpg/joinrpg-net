var paymentStatus = null;
var feeTotal = 0;
var feeBalance = 0;
var feeBase = 0;
var feeAccommodationDisp = null;
var feeClaimFieldsDisp = null;
var feeCharacterFieldsDisp = null;
var feeTotalFieldsDisp = null;
var feeTotalDisp = null;
var feeTotalDisp2 = null;
var feeCurrentDisp = null;
var feeModDisp = null;
var feeBalanceDisp = null;
var feeMoreToPayDisp = null;
var feeMoreToPayDisp2 = null;
var feeOverpaidDisp = null;
var feeOverpaidDisp2 = null;
var feePaymentDlgValue = null;
var rowPaymentStatus = null;

function setupFeeDisplay(obj)
{
    if (obj)
    {
        obj.get = function ()
        {
            return parseInt(this.innerHTML);
        };
        obj.set = function (value)
        {
            this.innerHTML = value;
        };
        obj.ch = function (oldV, newV)
        {
            this.set(this.get() - oldV + newV);
        };
    }
    return obj;
}

function setupFeeDisplayById(id)
{
    var obj = document.getElementById(id);
    return setupFeeDisplay(obj);
}

// Ensures that field with associated price has display element
function checkFeeElements(target)
{
    if (target.hasAttribute("price"))
    {
        var id = target.getAttribute("id");
        if (!target.feeDisp)
        {
            target.feeDisp = setupFeeDisplayById(id + "_disp");
            target.bound = target.getAttribute("bound");
        }

        return true;
    }
    return false;
}

// Checks current payment status and updates view according to it
function processPaymentStatus(paymentStatus)
{
    if (!rowPaymentStatus)
        return;

    $(rowPaymentStatus).removeClass("warning");
    $(rowPaymentStatus).removeClass("danger");
    $(rowPaymentStatus).removeClass("success");
    for (var i = 0; i < 4; i++)
    {
        $("#feeInfo" + i).toggle(i == paymentStatus);
        $("#feeStatus" + i).toggle(i == paymentStatus);
    }
    switch (paymentStatus)
    {
        case 0: // paid
            $(rowPaymentStatus).addClass("success");
            $(feeMoreToPayDisp).hide();
            $(feeOverpaidDisp).hide();
            if (feeTotal == 0)
            {
                $("#feeInfo0").hide();
                $("#feeStatus0").hide();
                $(rowPaymentStatus).hide();
            }
            break;
        case 1: // overpaid
            $(feeMoreToPayDisp).hide();
            $(feeOverpaidDisp).show();
            $(rowPaymentStatus).addClass("success");
            $(rowPaymentStatus).show();
            break;
        case 2: // more to pay
            $(feeMoreToPayDisp).show();
            $(feeOverpaidDisp).hide();
            $(rowPaymentStatus).addClass("warning");
            $(rowPaymentStatus).show();
            if (feePaymentDlgValue)
                feePaymentDlgValue.value = feeMoreToPayDisp.get();
            break;
        case 3: // not paid
            $(feeMoreToPayDisp).show();
            $(feeOverpaidDisp).hide();
            $(rowPaymentStatus).addClass("danger");
            $(rowPaymentStatus).show();
            if (feePaymentDlgValue)
                feePaymentDlgValue.value = feeMoreToPayDisp.get();
            break;
        default:
            break;
    }
}

// Applies fee modification
function feeChanged(target, fee)
{
    var oldFee = target.feeDisp.get();
    target.feeDisp.set(fee);

    // Updating subtotal of fields
    feeTotalFieldsDisp.ch(oldFee, fee);

    // Claim total    
    if (feeTotalDisp)
    {
        // If there is no feeTotalDisp, we are in character editing mode

        // Updating fields subtotals
        switch (target.bound)
        {
            case "Claim":
                feeClaimFieldsDisp.ch(oldFee, fee);
                break;
            case "Character":
                feeCharacterFieldsDisp.ch(oldFee, fee);
                break;
            default:
                break;
        }

        // Updating total amout to pay
        feeTotalDisp.ch(oldFee, fee);
        feeTotal = feeTotalDisp.get();
        feeTotalDisp2.set(feeTotal);

        // More to pay
        feeOverpaidDisp.set(feeBalance - feeTotal);
        feeOverpaidDisp2.set(feeBalance - feeTotal);
        // Overpaid
        feeMoreToPayDisp.set(feeTotal - feeBalance);
        feeMoreToPayDisp2.set(feeTotal - feeBalance);

        // Payment status
        if (feeTotal < feeBalance)
            paymentStatus = 1;
        else if (feeTotal == feeBalance)
            paymentStatus = 0;
        else if (feeBalance > 0)
            paymentStatus = 2;
        else
            paymentStatus = 3;

        processPaymentStatus(paymentStatus);
    }

    return oldFee;
}

// Number modification handler
function numberChanged(event)
{
    var target = event.target;
    if (checkFeeElements(target))
    {
        var price = parseInt(target.getAttribute("price"));
        feeChanged(target, price * parseInt(target.value));
    }
}

// Select modification handler
function selectChanged(event)
{
    var target = event.target;
    if (checkFeeElements(target))
    {
        var option = target.options[target.selectedIndex];
        feeChanged(target, parseInt(option.getAttribute("price")));
    }
}

// Multiselect modification handler
function multiChanged(event)
{
    var target = event.target;
    if (checkFeeElements(target))
    {
        var fee = 0, option, selected = false;

        // checking if we have the placeholder item
        var placeholder = target.options.length > 0 && target.options[0].value.trim().length === 0;
        var i = placeholder ? 1 : 0;

        // calculating total fee of selected items
        for (i = i; i < target.options.length; i++)
        {
            option = target.options[i];
            if (option.selected)
            {
                selected = true;
                fee += parseInt(option.getAttribute("price"));
            }
        }

        // must change selection of the placeholder item (if exists)
        if (placeholder)
        {
            target.options[0].selected = !selected;
            if (!selected)
                fee = parseInt(target.options[0].getAttribute("price"));
        }

        feeChanged(target, fee);
    }
}

// Checkbox modification handler
function checkChanged(event)
{
    var target = event.target;
    if (checkFeeElements(target))
    {
        feeChanged(target, target.checked ? parseInt(target.getAttribute("price")) : 0);
    }
}


function toggleFeeBlock()
{
    $("#feeInfo").collapse("toggle");
    $("#feeDetails").collapse("toggle");
}


$(function ()
{
    feeAccommodationDisp = setupFeeDisplayById("feeAccommodationDisp");
    feeClaimFieldsDisp = setupFeeDisplayById("feeClaimFieldsDisp");
    feeCharacterFieldsDisp = setupFeeDisplayById("feeCharacterFieldsDisp");
    feeTotalFieldsDisp = setupFeeDisplayById("feeTotalFieldsDisp");
    feeTotalDisp = setupFeeDisplayById("feeTotalDisp");
    feeTotalDisp2 = setupFeeDisplayById("feeTotalDisp2");
    feeCurrentDisp = setupFeeDisplayById("feeCurrentDisp");
    feeModDisp = setupFeeDisplayById("feeModDisp");
    feeBalanceDisp = setupFeeDisplayById("feeBalanceDisp");
    feeMoreToPayDisp = setupFeeDisplayById("feeMoreToPayDisp");
    feeMoreToPayDisp2 = setupFeeDisplayById("feeMoreToPayDisp2");
    feeOverpaidDisp = setupFeeDisplayById("feeOverpaidDisp");
    feeOverpaidDisp2 = setupFeeDisplayById("feeOverpaidDisp2");
    rowPaymentStatus = document.getElementById("rowPaymentStatus");

    feePaymentDlgValue = document.getElementById("Money");

    if (paymentStatus != null)
        processPaymentStatus(paymentStatus);
});
