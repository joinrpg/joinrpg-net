var feeClaimFieldsDisp = null;
var feeCharacterFieldsDisp = null;
var feeTotalFieldsDisp = null;
var feeTotalDisp = null;
var feeCurrentDisp = null;
var feeModDisp = null;
var feeBalanceDisp = null;
var feeMoreToPayDisp = null;
var feeOverpaidDisp = null;
var feePaymentDlgValue = null;
var feeStatusOverpaid = null;
var feeStatusPaid = null;
var feeStatusMoreToPay = null;

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
    $("#rowPaymentStatus").removeClass("warning");
    $("#rowPaymentStatus").removeClass("danger");
    $("#rowPaymentStatus").removeClass("success");
    switch (paymentStatus)
    {
        case 0: // paid
            $(feeStatusPaid).show();
            $(feeStatusOverpaid).hide();
            $(feeStatusMoreToPay).hide();
            $("#rowPaymentStatus").addClass("success");
            $(feeMoreToPayDisp).hide();
            $(feeOverpaidDisp).hide();
            break;
        case 1: // overpaid
            $(feeStatusPaid).hide();
            $(feeStatusOverpaid).show();
            $(feeStatusMoreToPay).hide();
            $(feeMoreToPayDisp).hide();
            $(feeOverpaidDisp).show();
            $("#rowPaymentStatus").addClass("success");
            break;
        case 2: // more to pay
            $(feeStatusPaid).hide();
            $(feeStatusOverpaid).hide();
            $(feeStatusMoreToPay).show();
            $(feeMoreToPayDisp).show();
            $(feeOverpaidDisp).hide();
            $("#rowPaymentStatus").addClass("warning");
            if (feePaymentDlgValue)
                feePaymentDlgValue.value = feeMoreToPayDisp.get();
            break;
        case 3: // not paid
            $(feeStatusPaid).hide();
            $(feeStatusOverpaid).hide();
            $(feeStatusMoreToPay).show();
            $(feeMoreToPayDisp).show();
            $(feeOverpaidDisp).hide();
            $("#rowPaymentStatus").addClass("danger");
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
        var feeTotal = feeTotalDisp.get();

        // Claim current
        //if (feeCurrentDisp)
        //    feeCurrentDisp.ch(oldFee, fee);
        // More to pay
        feeOverpaidDisp.set(feeBalance - feeTotal);
        // Overpaid
        feeMoreToPayDisp.set(feeTotal - feeBalance);

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

        //if (feeCurrentDisp)
        //{
        //    feeCurrentDisp.ch(oldFee, fee);
        //}
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


$(function ()
{
    feeClaimFieldsDisp = setupFeeDisplayById("feeClaimFieldsDisp");
    feeCharacterFieldsDisp = setupFeeDisplayById("feeCharacterFieldsDisp");
    feeTotalFieldsDisp = setupFeeDisplayById("feeTotalFieldsDisp");
    feeTotalDisp = setupFeeDisplayById("feeTotalDisp");
    feeCurrentDisp = setupFeeDisplayById("feeCurrentDisp");
    feeModDisp = setupFeeDisplayById("feeModDisp");
    feeBalanceDisp = setupFeeDisplayById("feeBalanceDisp");
    feeMoreToPayDisp = setupFeeDisplayById("feeMoreToPayDisp");
    feeOverpaidDisp = setupFeeDisplayById("feeOverpaidDisp");
    feeStatusMoreToPay = document.getElementById("feeStatusMoreToPay");
    feeStatusOverpaid = document.getElementById("feeStatusOverpaid");
    feeStatusPaid = document.getElementById("feeStatusPaid");

    feePaymentDlgValue = document.getElementById("Money");
    
    processPaymentStatus(paymentStatus);
});
