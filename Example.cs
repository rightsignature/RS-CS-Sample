using System;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;


namespace RightSignature
{
    public class Example
    {
        private const string apiToken = "YOUR TOKEN";                   // API Secure Token
        
        static void Main(string[] args)
        {
        RightSignatureAPI.debug = true;

            // Gets Documents from RightSignature
            getDocuments();

            // Gets Template Details
            //response = rightsignature.GetTemplateDetails("a_123_ed90909d0cc44d939ed6c9db79227421");

            // Use a Templates to send a document, can merge multiple Templates into one Document
            string[] templateGUIDs = new string[1] { "a_123_ed90909d0cc44d939ed6c9db79227421" };
            prepackage_and_send_template(templateGUIDs);


            // wait for any key press before exiting
            Console.WriteLine("\nPress Any Key to exit...");
            Console.ReadKey();
        }

        /********************************************************************
         * Sample calls to the api to get Documents from RightSignature
         * 1. Prepackge Template(s) to get a Document GUID
         * 2. Send/Prefill Document using GUID
         ********************************************************************/
        public static void getDocuments()
        {
            RightSignatureAPI rightsignature = new RightSignatureAPI(apiToken);
            XDocument response;

            // Tags to search for
            Dictionary<string, string> searchTags = new Dictionary<string, string>();
            // A tag with value
            searchTags.Add("document", "54");
            // A general tag (no value)
            searchTags.Add("test", null);

            // Calls API using our HTTPWebRequest wrapper to get a list of RightSignature Documents
            // Using test as the search query and default options
            //response = rightsignature.GetDocuments("test", null, null, null, null, null);

            // Get documents with specified tags and default options
            //response = rightsignature.GetDocuments(null, null, null, null, null, searchTags);
            // Get 2nd page of documents with default options and tags
            //response = rightsignature.GetDocuments(null, null, 2, null, null, searchTags);
            // Get documents from "alex@example.com"
            //response = rightsignature.GetDocuments(null, null, null, null, "alex@example.com", null);

            // Test getting document details from a guid
            //response = rightsignature.GetDocumentDetails("NYWGKXIYDJPHK6DLW5WXCZ");

            // Calls API using our HTTPWebRequest wrapper to get a list of RightSignature Templates
            //response = rightsignature.GetTemplates(null, null, null, null);

        }

        /********************************************************************
         * Workflow for using Templates to send a Document:
         * 1. Prepackge Template(s) to get a Document GUID
         * 2. Send/Prefill Document using GUID
         ********************************************************************/
        public static void prepackage_and_send_template(string[] templateGUIDs)
        {
            RightSignatureAPI rightsignature = new RightSignatureAPI(apiToken);
            XDocument response;

            // Prepackge a Template to prepare it for sending, sets a Callback URL so we can get listen for callbacks 
            //  when the Documenet gets created, viewed, and completed (all signers signed)
            string guid = rightsignature.PrepackageTemplate(templateGUIDs, "http://127.0.0.1:8888");
            Console.WriteLine("got GUID:" + guid);

            //Creating array for the mergeFields
            RightSignatureAPI.MergeField[] mergeFields = new RightSignatureAPI.MergeField[2] { 
                new RightSignatureAPI.MergeField("Notes", "custom Notes here", true), 
                new RightSignatureAPI.MergeField("Notes 2", "Notes sections 2", true)
            };

            // Filling in the info for Document Roles
            Dictionary<string, RightSignatureAPI.RoleUser> roles = new Dictionary<string, RightSignatureAPI.RoleUser>();
            roles.Add("Document Sender", new RightSignatureAPI.RoleUser("Johnny John", "jj@example.com", true));
            roles.Add("Designer", new RightSignatureAPI.RoleUser("James Able", "ja@example.com", true));
            roles.Add("Project Manager", new RightSignatureAPI.RoleUser("Jim Brown", "jb@example.com", true));

            // Create tags to associate with Document
            Dictionary<string, string> tags = new Dictionary<string,string>();
            tags.Add("test", null);
            tags.Add("user", "123");

            // Send document with fields filled out
            // (guid, subject, roles, mergeFields, tags, description, callbackURL, expires_in)
            response = rightsignature.SendDocument(guid, "Subject", roles, mergeFields, tags, "Please fill out the information form and submit it with your signature.", "http://127.0.0.1:3000", 2);

            Console.WriteLine("Response received is:\n" + response.ToString());
        }
    }
}