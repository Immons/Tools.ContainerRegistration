namespace Tools.ContainerRegistration.Attributes
{
	/// <summary>
	/// Specifies a callback method to be invoked after the service is resolved from the container.
	/// The callback method should be a static method that accepts the resolved instance as parameter.
	/// Example: [OnActivated(typeof(MyLocator), nameof(MyLocator.SetImplementation))]
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class OnActivatedAttribute : Attribute
	{
		/// <summary>
		/// Creates an OnActivated callback using a static method from another type.
		/// </summary>
		/// <param name="targetType">The type containing the static method</param>
		/// <param name="methodName">The name of the static method to call</param>
		public OnActivatedAttribute(Type targetType, string methodName)
		{
			TargetType = targetType;
			MethodName = methodName;
		}

		/// <summary>
		/// Creates an OnActivated callback using an instance method on the resolved service.
		/// </summary>
		/// <param name="methodName">The name of the instance method to call on the resolved service</param>
		public OnActivatedAttribute(string methodName)
		{
			MethodName = methodName;
			IsInstanceMethod = true;
		}

		public Type? TargetType { get; }
		public string MethodName { get; }
		public bool IsInstanceMethod { get; }
	}
}
