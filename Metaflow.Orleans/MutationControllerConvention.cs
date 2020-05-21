using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Metaflow.Orleans
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MutationControllerConvention : Attribute, IControllerModelConvention
    {
        public string FormatPattern { get; set; }

        public void Apply(ControllerModel controller)
        {
            if (NotApplicable(controller)) return;

            var grainType = controller.ControllerType.GenericTypeArguments[0];

            var requestType = controller.ControllerType.GenericTypeArguments[1];

            var argType = controller.ControllerType.GenericTypeArguments[2];

            controller.ControllerName = grainType.Name;

            controller.Selectors[0].AttributeRouteModel.Template = controller.ControllerName.ToLowerInvariant();

            var template = argType == grainType ? "{id}" : "{id}/" + argType.Name.ToLowerInvariant();


            HttpMethodAttribute method = (HttpMethodAttribute) Activator.CreateInstance(requestType, template);

            var s = new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel(method)
            };

            s.ActionConstraints.Add(new HttpMethodActionConstraint(method.HttpMethods)
            {

            });

            if (s != null)
            {
                controller.Actions.First().Selectors.Clear();
                controller.Actions.First().Selectors.Add(s);
            }
        }

        private static bool NotApplicable(ControllerModel controller)
        {
            return !controller.ControllerType.IsGenericType || controller.ControllerType.GetGenericTypeDefinition() != typeof(MutationController<,,>);
        }
    }
}
