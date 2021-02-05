using Fclp;
using Newtonsoft.Json;
using Salesforce.Force;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace SalesforcePocoGenerator
{
    class Program
    {
        public class ApplicationArguments
        {
            public string ConsumerKey { get; set; }
            public string ConsumerSecret { get; set; }
            public string UserName { get; set; }
            public string PasswordAndToken { get; set; }
            public string TokenUrl { get; set; }
            public string ObjectApiName { get; set; }

            public string Namespace { get; set; }
            public string FileOutput { get; set; }
        }
        static ApplicationArguments SetupCommandLineParser(string[] args)
        {
            var p = new FluentCommandLineParser<ApplicationArguments>();

            p.Setup(arg => arg.ConsumerKey)
             .As("ConsumerKey") 
             .Required();

            p.Setup(arg => arg.ConsumerSecret)
             .As("ConsumerSecret")
             .Required();

            p.Setup(arg => arg.UserName)
             .As("UserName")
             .Required();

            p.Setup(arg => arg.PasswordAndToken)
             .As("PasswordAndToken")
             .Required();

            p.Setup(arg => arg.TokenUrl)
              .As("TokenUrl")
              .Required();

            p.Setup(arg => arg.ObjectApiName)
              .As("ObjectApiName")
              .Required();

            p.Setup(arg => arg.Namespace)
              .As("Namespace")
              .Required();

            p.Setup(arg => arg.FileOutput)
              .As("FileOutput");

            var result = p.Parse(args);
            if(result.HasErrors)
            {
                throw new Exception(result.ErrorText);
            }

            return p.Object;
        }

        static async Task Main(string[] args)
        {
            var appArguments = SetupCommandLineParser(args);
            Console.WriteLine("Salesforce Object to Poco Generator for Rest API calls.");

            ForceClient forceClient = null;
            using (var auth = new Salesforce.Common.AuthenticationClient())
            {
                await auth.UsernamePasswordAsync(appArguments.ConsumerKey, appArguments.ConsumerSecret, appArguments.UserName, appArguments.PasswordAndToken, appArguments.TokenUrl);

                forceClient = new ForceClient(auth.InstanceUrl, auth.AccessToken, "v50.0");
            }
            // get object description from Salesforce API
            var describeObjectInfo = await forceClient.DescribeAsync<SalesforceObjectDescribe>(appArguments.ObjectApiName);

            // generate c# class into string
            var classStr = ($@"
            namespace {appArguments.Namespace}
            {{
                public class {appArguments.ObjectApiName}
                {{
                    {string.Join("\r\n", describeObjectInfo.Fields.Select(x=>
                      $" public {GetCSharpType(x.SoapType, x.Nillable)} {x.Name} {{get; set;}} "                                  
                    ))
                    }
                }}
            }}
            ");

            Console.WriteLine(classStr);

            // write to file
            if(! string.IsNullOrEmpty(appArguments.FileOutput))
            {
                File.WriteAllText(appArguments.FileOutput, "");
            }

        }

        private static string GetCSharpType(string soapType, bool nillable)
        {
            switch(soapType)
            {
                case "tns:ID":
                case "xsd:ID":
                case "xsd:IDREF":
                case "xsd:IDREFS":
                case "xsd:language":
                case "xsd:Name":
                case "xsd:NMTOKEN":
                case "xsd:NMTOKENS":
                case "xsd:normalizedString":
                case "xsd:QName":
                case "xsd:token":
                case "xsd:string":
                case "xsd:anyURI":
                case "xsd:NOTATION":
                    return "string";
                    break;
                case "xsd:dateTime":
                case "xsd:date":
                    return nillable ? "DateTime?" : "DateTime";
                    break;
                case "xsd:decimal":
                    return nillable ? "decimal?" : "decimal";
                    break;
                case "xsd:byte":
                case "xsd:unsignedByte":
                    return nillable ? "byte?" : "byte";
                    break;
                case "xsd:int":
                case "xsd:negativeInteger":
                case "xsd:nonNegativeInteger":
                case "xsd:nonPositiveInteger":
                case "xsd:positiveInteger":
                    return nillable ? "int?" : "int";
                    break;
                case "xsd:short":
                    return nillable ? "short?" : "short";
                    break;
                case "xsd:unsignedLong":
                    return nillable ? "ulong?" : "ulong";
                    break;
                case "xsd:unsignedInt":
                    return nillable ? "uint?" : "uint";
                    break;
                case "xsd:unsignedShort":
                    return nillable ? "ushort?" : "ushort";
                    break;

                case "xsd:double":
                    return nillable ? "double?" : "double";
                    break;
                case "xsd:float":
                    return nillable ? "float?" : "float";
                    break;



                case "xsd:boolean":
                    return nillable ? "bool?" : "bool";
                    break;

                case "xsd:base64Binary":
                case "xsd:hexBinary":
                    return "byte[]";
                    break;

            }

            return "object";
            
        }
    }
}
