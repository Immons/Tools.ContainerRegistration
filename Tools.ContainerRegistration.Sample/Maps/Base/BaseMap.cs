using MapsterMapper;
using Tools.ContainerRegistration.Sample.Common.Maps.Interfaces;

namespace Tools.ContainerRegistration.Sample.Maps.Base
{
	public class BaseMap : IMap
	{
		protected readonly IMapper Mapper;

		protected BaseMap(IMapper mapper)
		{
			Mapper = mapper;
		}

		public virtual void Register() {}
	}
}