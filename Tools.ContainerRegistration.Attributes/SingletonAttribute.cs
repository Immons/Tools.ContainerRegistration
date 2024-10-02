namespace Tools.ContainerRegistration.Attributes
{
	/// <summary>
	/// Singleton has priority before scoped
	/// </summary>
	public class SingletonAttribute : Attribute
	{
		public SingletonAttribute(bool autoActivate = false)
		{
			AutoActivate = autoActivate;
		}

		public bool AutoActivate { get; }
	}
}
