namespace Tools.ContainerRegistration.Attributes
{
	public class SingletonAttribute : Attribute
	{
		public SingletonAttribute(bool autoActivate = false)
		{
			AutoActivate = autoActivate;
		}

		public bool AutoActivate { get; }
	}
}
