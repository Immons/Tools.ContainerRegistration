using Tools.ContainerRegistration.Attributes;

namespace Tools.ContainerRegistration.Sample.ViewModels;

/// <summary>
/// will be registered although it exist in in ioc_config.json in "ExcludedFromRegisteringTypesEndingWith",
/// because it contains attribute
/// It will also be registered as IBaseUserViewModel and IFriendsViewModel due to ServiceRegistration attribute
/// </summary>
[Singleton]
[ServiceRegistration([typeof(IBaseUserViewModel), typeof(IFriendsViewModel)])]
public class FriendlyUserViewModel : IBaseUserViewModel, IFriendsViewModel
{
}