using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// <see cref="InputNumber{TValue}"/> с классом <c>form-control</c>, на которое ссылается подпись <see cref="FormRow"/>.
/// </summary>
public class JoinInputNumber<TValue> : InputNumber<TValue>
{
    [CascadingParameter]
    private FormRowLabelTarget? LabelTarget { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var attributes = new Dictionary<string, object>(AdditionalAttributes ?? new Dictionary<string, object>());
        attributes.TryAdd("class", "form-control");
        if (LabelTarget?.Claim(this, FormRowLabelTarget.ExplicitId(AdditionalAttributes)) is { } id)
        {
            attributes["id"] = id;
        }
        AdditionalAttributes = attributes;
    }
}
