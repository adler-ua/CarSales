using System.Security.Cryptography;
using Duende.IdentityServer.Models;

namespace IdentityService;

public static class Config
{
    // using OpenId Connect means that we are going get two tockens back:
    // id-token, that contains information about the user
    // access-token -- the key that allows a client to request resources from our resource server (AuctionApi in our case)
        
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("auctionApp", "Auction app full access")
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // this is only for development purposes
            new Client
            {
                ClientId = "postman",
                ClientName = "Postman",
                AllowedScopes =  {"openid", "profile", "auctionApp"},
                RedirectUris = {"https://www.getpostman.com/oath2/callback/whateveryouwantendpoint-forpostman"},
                ClientSecrets = new [] { new Secret("NotASecret".Sha256()) },
                AllowedGrantTypes = { GrantType.ResourceOwnerPassword }
            },
             new Client
             {
                ClientId = "nextApp",
                ClientName = "nextApp",
                ClientSecrets = {new Secret("secret-secret".Sha256())},
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                RequirePkce = false, // would set to true for the native mobile app
                RedirectUris = {"http://localhost:3000/api/auth/callback/id-server"},
                AllowOfflineAccess = true,
                AllowedScopes = {"openid", "profile", "auctionApp"},
                AccessTokenLifetime = 3600*24*30, // 1 month
             }
        };
}
