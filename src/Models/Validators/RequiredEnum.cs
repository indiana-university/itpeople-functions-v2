using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models
{
	/*
	 * Enum's always default to 0, which is fine when we intend for them to have a default
	 * value, but in cases where we don't it's a problem.
	 * If you strictly enumerate your options starting at 1, when it defaults to 0
	 * this attribute will throw a well-formed error message.
	 */
	public class RequiredEnum : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			var required = new ValidationResult("The " + validationContext.DisplayName + " field is required.");

			var prop = validationContext.ObjectType.GetProperty(validationContext.MemberName);
			var curValue = (int)value;
			
			var options = Enum.GetValues(prop.PropertyType).Cast<int>().ToList();

			if(options.Contains(curValue) == false)
			{
				return required;
			}
			
			return null;
		}
	}
}