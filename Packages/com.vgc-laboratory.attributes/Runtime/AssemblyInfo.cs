using System.Runtime.CompilerServices;

// ExecuteScopeHelper 等の internal を Udon 側の Executor から参照するため
[assembly: InternalsVisibleTo("VGC.Attributes.Udon.Runtime")]
