﻿namespace Microsoft.AspNetCore.Mvc
{
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    sealed class ODataActionDescriptorProvider : IActionDescriptorProvider
    {
        readonly IODataRouteCollectionProvider routeCollectionProvider;
        readonly ApplicationPartManager partManager;
        readonly IModelMetadataProvider modelMetadataProvider;

        public ODataActionDescriptorProvider(
            IODataRouteCollectionProvider routeCollectionProvider,
            ApplicationPartManager partManager,
            IModelMetadataProvider modelMetadataProvider )
        {
            Contract.Requires( routeCollectionProvider != null );
            Contract.Requires( partManager != null );
            Contract.Requires( modelMetadataProvider != null );

            this.routeCollectionProvider = routeCollectionProvider;
            this.partManager = partManager;
            this.modelMetadataProvider = modelMetadataProvider;
        }

        public int Order => 0;

        public void OnProvidersExecuted( ActionDescriptorProviderContext context )
        {
            Contract.Requires( context != null );

            if ( routeCollectionProvider.Items.Count == 0 )
            {
                return;
            }

            var results = context.Results.ToArray();
            var conventions = new IODataActionDescriptorConvention[]
            {
                new ImplicitHttpMethodConvention(),
                new ODataRouteBindingInfoConvention( routeCollectionProvider, modelMetadataProvider ),
            };

            foreach ( var action in ODataActions( results ) )
            {
                foreach ( var convention in conventions )
                {
                    convention.Apply( context, action );
                }
            }
        }

        public void OnProvidersExecuting( ActionDescriptorProviderContext context ) { }

        static IEnumerable<ControllerActionDescriptor> ODataActions( IEnumerable<ActionDescriptor> results )
        {
            Contract.Requires( results != null );
            Contract.Ensures( Contract.Result<IEnumerable<ControllerActionDescriptor>>() != null );

            foreach ( var result in results )
            {
                if ( result is ControllerActionDescriptor action &&
                     action.ControllerTypeInfo.IsODataController() &&
                    !action.ControllerTypeInfo.IsMetadataController() )
                {
                    yield return action;
                }
            }
        }
    }
}