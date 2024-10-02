namespace Tools.ContainerRegistration.Sample.ViewModels;

/// <summary>
/// will be registered as ProfileViewModel because exist in ioc_config.json in "RegisterTypesEndingWith" but
/// won't be registered as IProfileViewModel because it also exists in "ExcludedFromRegisteringAsConventionInterface"
/// </summary>
public class ProfileViewModel : IProfileViewModel
{
}