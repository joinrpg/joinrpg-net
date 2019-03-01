using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace JoinRpg.Helpers.Web
{
    public class BooleanRequired : RequiredAttribute, IClientValidatable
    {
        public override bool IsValid(object value)
        {
            return value != null && (bool) value;
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            ModelMetadata metadata,
            ControllerContext context)
        {
            return new ModelClientValidationRule[]
            {
                new ModelClientValidationRule()
                    {ValidationType = "brequired", ErrorMessage = this.ErrorMessage}
            };
        }
    }
}
