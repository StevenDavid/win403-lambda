using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Newtonsoft.Json;

namespace win403.Models
{
  public class Helpers
  {
    public static string SecretKey = "testdb1_CS";
    public static string region = "us-east-1";

    public static string currentMSSQL_CS = "";
    public static string currentAurora_CS = "";
    public static string currentAuroraSL_CS = "";
    public static string GetMSSQLConnectionString()
    {
      if (currentMSSQL_CS == "") {
        string rawCS = GetSecret(SecretKey);
        DB_CS currentCSObject = JsonConvert.DeserializeObject<DB_CS>(rawCS);
        currentMSSQL_CS = $"Data Source={currentCSObject.host};User ID={currentCSObject.username};Password={currentCSObject.password}";
      }
      return currentMSSQL_CS;
    }

    public static string GetAuroraServerlessConnectionString()
    {
     if (currentAuroraSL_CS == "") {
        string rawCS = GetSecret("dbtest/aurora/serverless");
        DB_CS currentCSObject = JsonConvert.DeserializeObject<DB_CS>(rawCS);
        currentAuroraSL_CS = $"Data Source={currentCSObject.host};port={currentCSObject.port};User ID={currentCSObject.username};Password={currentCSObject.password};";
      }
      return currentAuroraSL_CS;
    }

     public static string GetAuroraConnectionString()
    {
      if (currentAurora_CS == "") {
        string rawCS = GetSecret("aurora_static");
        DB_CS currentCSObject = JsonConvert.DeserializeObject<DB_CS>(rawCS);
        currentAurora_CS = $"Data Source={currentCSObject.host};port={currentCSObject.port};User ID={currentCSObject.username};Password={currentCSObject.password};";
      }
      return currentAurora_CS;
    }

    public static string GetSecret(string secretName)
    {
        
        string secret = "";

        MemoryStream memoryStream = new MemoryStream();

        IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

        GetSecretValueRequest request = new GetSecretValueRequest();
        request.SecretId = secretName;
        request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.

        GetSecretValueResponse response = null;

        // In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
        // See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
        // We rethrow the exception by default.

        try
        {
            response = client.GetSecretValueAsync(request).Result;
        }
        catch (Exception exp)
        {
            // More than one of the above exceptions were triggered.
            // Deal with the exception here, and/or rethrow at your discretion.
            Console.WriteLine("error: " + exp.Message);
        }

            Console.WriteLine("Made it past the try / catch blocks");
        // Decrypts secret using the associated KMS CMK.
        // Depending on whether the secret is a string or binary, one of these fields will be populated.
        if (response.SecretString != null)
        {
            Console.WriteLine("Secret String was not Null" + response.SecretString);
            secret = response.SecretString;
        }
        else
        {
            Console.WriteLine("Secret String was Null");
            memoryStream = response.SecretBinary;
            StreamReader reader = new StreamReader(memoryStream);
            string decodedBinarySecret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
        }

        return secret;
    }

  }

}