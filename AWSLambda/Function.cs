using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda;

public class Function
{
    private readonly DynamoDBContext _dynamoDbContext;
    public Function()
    {
        _dynamoDbContext = new DynamoDBContext(new AmazonDynamoDBClient());
    }

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        return request.RequestContext.Http.Method.ToUpper() switch
        {
            "GET" => await HandlerGetRequest(request, context),
            "POST" => await HandlerPostRequest(request, context),
            "DELETE" => await HandlerDeleteRequest(request, context),
        };
       
    }
    private async Task<APIGatewayHttpApiV2ProxyResponse> HandlerGetRequest(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        request.PathParameters.TryGetValue("userid", out var userIdString);
        if (Guid.TryParse(userIdString, out var userid))
        {
            var user = await _dynamoDbContext.LoadAsync<User>(userid);
            if (user != null)
            {
                return new APIGatewayHttpApiV2ProxyResponse()
                {
                    StatusCode = 200,
                    Body = JsonSerializer.Serialize(user)
                };
            }
        }
        return BadResponse("Invalid userId in path");

    }
    private  async Task<APIGatewayHttpApiV2ProxyResponse> HandlerPostRequest(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var user =JsonSerializer.Deserialize<User>(request.Body);
        if (user == null)
        {
            return BadResponse("Invalid user details");
        }
        await _dynamoDbContext.SaveAsync(user);
        return OKResponse();
    }
    private async Task<APIGatewayHttpApiV2ProxyResponse> HandlerDeleteRequest(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        request.PathParameters.TryGetValue("userid", out var userIdString);
        if (Guid.TryParse(userIdString, out var userid))
        {
            await _dynamoDbContext.DeleteAsync<User>(userid);
            return OKResponse();
            
        }
        return BadResponse("Invalid userId in path");

    }

    private static APIGatewayHttpApiV2ProxyResponse BadResponse(string message)
    {
        return new APIGatewayHttpApiV2ProxyResponse()
        {
            StatusCode = 404,
            Body = message
        };
    }
    private static APIGatewayHttpApiV2ProxyResponse OKResponse()
    {
        return new APIGatewayHttpApiV2ProxyResponse()
        {
            StatusCode = 200,
        };
    }
    public  class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
