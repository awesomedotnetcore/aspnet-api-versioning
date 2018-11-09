﻿namespace Microsoft.AspNet.OData.Basic
{
    using FluentAssertions;
    using Microsoft.AspNet.OData.Basic.Controllers;
    using Microsoft.AspNet.OData.Builder;
    using Microsoft.AspNet.OData.Configuration;
    using Microsoft.OData.UriParser;
    using Microsoft.Web;
    using Microsoft.Web.OData.Basic.Controllers;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Xunit;
    using static Microsoft.OData.ServiceLifetime;
    using static System.Net.HttpStatusCode;

    public abstract class BasicAcceptanceTest : ODataAcceptanceTest
    {
        protected BasicAcceptanceTest()
        {
            FilteredControllerTypes.Add( typeof( OrdersController ) );
            FilteredControllerTypes.Add( typeof( PeopleController ) );
            FilteredControllerTypes.Add( typeof( People2Controller ) );
            FilteredControllerTypes.Add( typeof( CustomersController ) );

            Configuration.AddApiVersioning( options => options.ReportApiVersions = true );

            var modelBuilder = new VersionedODataModelBuilder( Configuration )
            {
                ModelConfigurations =
                {
                    new PersonModelConfiguration(),
                    new OrderModelConfiguration(),
                    new CustomerModelConfiguration(),
                }
            };
            var models = modelBuilder.GetEdmModels();

            Configuration.MapVersionedODataRoutes( "odata", "api", models, builder => builder.AddService( Singleton, typeof( ODataUriResolver ), sp => TestUriResolver ) );
            Configuration.MapVersionedODataRoutes( "odata-bypath", "v{apiVersion}", models, builder => builder.AddService( Singleton, typeof( ODataUriResolver ), sp => TestUriResolver ) );
            Configuration.EnsureInitialized();
        }

        [Fact]
        public async Task then_service_document_should_return_400_for_unsupported_url_api_version()
        {
            // arrange
            var requestUrl = "v4";

            // act
            var response = await Client.GetAsync( requestUrl );
            var content = await response.Content.ReadAsAsync<OneApiErrorResponse>();

            // assert
            response.StatusCode.Should().Be( BadRequest );
            content.Error.Code.Should().Be( "UnsupportedApiVersion" );
        }

        [Theory]
        [InlineData( "?additionalQuery=true" )]
        [InlineData( "?additionalQuery=true#anchor-123" )]
        [InlineData( "#anchor-123" )]
        public async Task then_the_service_document_should_return_only_path_for_an_unsupported_version( string additionalUriPart )
        {
            // arrange
            var requestUrl = $"v4{additionalUriPart}";

            // act
            var response = await Client.GetAsync( requestUrl );
            var content = await response.Content.ReadAsAsync<OneApiErrorResponse>();


            // assert
            response.StatusCode.Should().Be( BadRequest );
            content.Error.Code.Should().Be( "UnsupportedApiVersion" );
            content.Error.Message.Should().Contain( "v4" );
            content.Error.Message.Should().NotContain( additionalUriPart );
        }

        [Fact]
        public async Task then_X24metadata_should_return_400_for_unsupported_url_api_version()
        {
            // arrange

            // act
            var response = await Client.GetAsync( "v4/$metadata" );
            var content = await response.Content.ReadAsAsync<OneApiErrorResponse>();

            // assert
            response.StatusCode.Should().Be( BadRequest );
            content.Error.Code.Should().Be( "UnsupportedApiVersion" );
        }
    }
}