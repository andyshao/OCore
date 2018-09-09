using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using OCore.Environment.Shell.Builders.Models;
using OCore.Modules;

namespace OCore.Mvc
{
    /// <summary>
    /// An <see cref="ApplicationPart"/> backed by an <see cref="Assembly"/>.
    /// </summary>
    public class ShellFeatureApplicationPart :
        ApplicationPart,
        IApplicationPartTypeProvider,
        ICompilationReferencesProvider
    {
        private static IEnumerable<string> _referencePaths;
        private static object _synLock = new object();

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initalizes a new <see cref="AssemblyPart"/> instance.
        /// </summary>
        /// <param name="assembly"></param>
        public ShellFeatureApplicationPart(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override string Name
        {
            get
            {
                return nameof(ShellFeatureApplicationPart);
            }
        }

        /// <inheritdoc />
        public IEnumerable<TypeInfo> Types
        {
            //get
            //{
            //    var shellBluePrint = _httpContextAccessor.HttpContext.RequestServices?.GetRequiredService<ShellBlueprint>();
            //    return shellBluePrint?.Dependencies.Keys.Select(type => type.GetTypeInfo()) ?? Enumerable.Empty<TypeInfo>();
            //}

            get {
                var services = _httpContextAccessor.HttpContext.RequestServices;

                if (services == null) {
                    return Enumerable.Empty<TypeInfo>();
                }

                var tagHelpers = services.GetServices<ITagHelpersProvider>();
                var shellBluePrint = services.GetRequiredService<ShellBlueprint>();

                return shellBluePrint.Dependencies.Keys.Select(type => type.GetTypeInfo())
                    .Concat(tagHelpers.SelectMany(p => p.Types));
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetReferencePaths()
        {
            if (_referencePaths != null)
            {
                return _referencePaths;
            }

            lock (_synLock)
            {
                if (_referencePaths != null)
                {
                    return _referencePaths;
                }

                _referencePaths = DependencyContext.Default.CompileLibraries
                    .SelectMany(library => library.ResolveReferencePaths());
            }

            return _referencePaths;
        }
    }
}