using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.IO;
using System.Text;

namespace RightSignature
{
    public class RightSignatureAPI
    {
        string baseUrl;
        string apiToken;
        // response buffer
        StringBuilder sb;
        byte[] buf;
        public static Boolean debug;

        public RightSignatureAPI(string inputApiToken) {
            // initialize response buffer
            sb = new StringBuilder();
            buf = new byte[8192];

            baseUrl = "https://rightsignature.com";
            apiToken = inputApiToken;
        }

        /*****************************************************************************
         * Structs to help filing out documents
         ****************************************************************************/
        // A Signer or CC of a Document
        // name - name of the Person for the role
        // email - email of the Person for the role
        // locked - values can be changed
        public struct RoleUser
        {
            public string name, email; 
            public Boolean locked;         // If true, not allow the redirected user to modify the value
            public RoleUser(string uName, string uEmail, Boolean isLocked) {
                name    = uName;
                email   = uEmail;
                locked  = isLocked;
            }
        }

        public struct MergeField {
            public string name, value;
            public Boolean locked;         // If true, not allow the redirected user to modify the value
            public MergeField(string mName, string mValue, Boolean isLocked){
                name    = mName;
                value   = mValue;
                locked  = isLocked;
            }
        }

        /******************************************************************************
         * PRIVATE FUNCTIONS
         ******************************************************************************/



        /******************************************************************************
         * Gets Documents from API and returns response as XDocument
         * 
	     * query (optional) = Search term to narrow results. Should be URI encoded.
	     * 		ex. "State Street"
	     * state (optional) - Comma-joined Document states to filter results. States should be 'completed', 'pending', 'expired'.
	     * 		ex. "completed,pending"
	     * page (optional) - Page number offset. Default is 1.
	     * 		ex. 1
	     * perPage (optional) - number of result per page to return.
	     * 		Valid values are 10, 20, 30, 40, and 50. Default is 10.
	     * 		ex. 20
	     * recipientEmail (optional) = Narrow results to documents sent by RightSignature API User to the given Recipient Email.
	     * 		ex. "a@abc.com"
	     * tags (optional) - Dictionary tag names and values that are associated with documents. 
	     * 		ex. Dictionary<string, string> tags = new Dictionary;
         * 		tags.Add('customized', '')
         * 		tags.Add('user id', '123')
         ******************************************************************************/
        public XDocument GetDocuments(string query, string docStates, int? page, int? perPage, string recipientEmail, Dictionary<string, string> tags)
        {
            string urlPath = "/api/documents.xml";
            string requestPath;
            List<string> queryParams = new List<string>();
            
            // Build up the URL request path parameters
            if (query != null)
                queryParams.Add("search=" + query);
            if (docStates != null)
                queryParams.Add("state=" + docStates);
            if (page.HasValue)
                queryParams.Add("page=" + page.ToString());
            if (perPage.HasValue)
                queryParams.Add("per_page=" + perPage.ToString());
            if (recipientEmail != null)
                queryParams.Add("recipient_email=" + recipientEmail.ToString());

            // Creates parameter string for tags
            if (tags != null) 
                queryParams.Add(CreateTagsParameter(tags));

            // Creates URL path with query parameters in it
            requestPath = CreateRequestPath(urlPath, queryParams);

            // Creates HTTP Request and parses it as XDocument
            return ParseResponseAsXML(HttpRequest(requestPath, "GET", null));
        }

        /******************************************************************************
         * Gets Document details from API and returns response as XDocument
         * 
         * guid - RightSignature Document GUID
         *      ex. 'J1KHD2NX4KJ5S6X7S8'
         ******************************************************************************/
        public XDocument GetDocumentDetails(string guid)
        {
            return ParseResponseAsXML(HttpRequest("/api/documents/" + guid + ".xml", "GET", null));
        }

        /******************************************************************************
         * Gets Documents from API and returns response as string
         ******************************************************************************/
        public string GetDocumentsString()
        {
            return ParseResponseAsString(HttpRequest("/api/documents.xml", "GET", null));
        }


        /******************************************************************************
         * Gets Templates from API and returns response as XDocument
         * 
         * query (optional) = Search term to narrow results. Should be URI encoded.
         * 		ex. "State Street"
         * page (optional) - Page number offset. Default is 1.
         * 		ex. 1
         * perPage (optional) - number of result per page to return.
         * 		Valid values are 10, 20, 30, 40, and 50. Default is 10.
         * 		ex. 20
         * tags (optional) - Dictionary tag names and values that are associated with documents. 
         * 		ex. Dictionary<string, string> tags = new Dictionary;
         * 		tags.Add('test', null)
         * 		tags.Add('user id', '123')
         ******************************************************************************/
        public XDocument GetTemplates(string query, int? page, int? perPage, Dictionary<string, string> tags)
        {
            string urlPath = "/api/templates.xml";
            string requestPath;
            List<string> queryParams = new List<string>();

            // Build up the URL request path parameters
            if (query != null)
                queryParams.Add("search=" + query);
            if (page.HasValue)
                queryParams.Add("page=" + page.ToString());
            if (perPage.HasValue)
                queryParams.Add("per_page=" + perPage.ToString());

            // Creates parameter string for tags
            if (tags != null)
                queryParams.Add(CreateTagsParameter(tags));

            // Creates URL path with query parameters in it
            requestPath = CreateRequestPath(urlPath, queryParams);

            // Creates HTTP Request and parses it as XDocument
            return ParseResponseAsXML(HttpRequest(requestPath, "GET", null));
        }

        /******************************************************************************
         * Gets Template details from API and returns response as XDocument
         * 
         * guid - RightSignature Template GUID
         *      ex. 'A_123_J1KHD2NX4KJ5S6X7S8'
         ******************************************************************************/
        public XDocument GetTemplateDetails(string guid)
        {
            return ParseResponseAsXML(HttpRequest("/api/templates/" + guid + ".xml", "GET", null));
        }

        /******************************************************************************
         * Prepackages 1 or more Templates so it creates a RightSignature Document from the RightSignature Templates.
         *  Returns GUID for new Document
         * 
         * guids - RightSignature Template GUID
         *      ex. 'A_123_J1KHD2NX4KJ5S6X7S8'
         * callbackURL (optional) - URL to callback when the Document is created. 
         *               If none is specified, the default in RightSignature's Account settings will be used
         *      ex. "http://mysite/template_callback.php"
         ******************************************************************************/
        public string PrepackageTemplate(string[] guids, string callbackURL)
        {
            string guidsString = "";
            foreach (string guid in guids) {
                guidsString += guid;
                guidsString += ",";
            }

            // XML body to POST to RightSignature API
            string data = "<?xml version='1.0' encoding='UTF-8'?><template>";
            if (callbackURL != null)
                data += "<callback_location>" + callbackURL + "</callback_location>";
            data += "</template>";

            XDocument response = ParseResponseAsXML(HttpRequest("/api/templates/" + guidsString + "/prepackage.xml", "POST", data));

            RightSignatureAPI.log("Prepackage Response\n" + response.ToString());

            // TODO: Need to Check if there's an Error response in XML


            return response.Element("template").Element("guid").Value;
        }

        /******************************************************************************
         * Sends Document from API and returns response as XDocument
         * 
         * guid - RightSignature's Dcoument GUID
         * 		ex. "AKJ8CUID2D34TFS"
         * roles - Dictionary of RoleUser structs with the Key being the Role Name in the Document:
         * 		ex. Dictionary<string, string> roles = new Dictionary;
         * 		tags.Add('Client', RoleUser("Tim Tam Timmy", "tim@example.com", true))
         * 		tags.Add('CoSigner', RoleUser("Jim Jam Jammy", "jim@example.com", false))
         * mergeFields - Array of MergeField structs the name must map to MergeField names in the Document. 
         * 		ex. [MergeField('Address', "123 Maple Lane", false)]
         * tags (optional) - Dictionary tag names and values to associate with Document. 
         * 		ex. Dictionary<string, string> tags = new Dictionary;
         * 		tags.Add('test', null)
         * 		tags.Add('user id', '123')
         * description (Optional) - description of document for signer to see
         * callbackURL (Optional) - string of URL for RightSignature to POST document details to after Template gets created, viewed, and completed (all parties have signed). 
         *      Tip: add a unique parameter in the URL to distinguish each callback, like the template_id.
         *      NULL will use the default callback url set in the RightSignature Account settings page (https://rightsignature.com/oauth_clients).
         *      ex. 'http://mysite/document_callback.php?template_id=123'
         * expires_in (Optional) - integer of days to expire document, allowed values are 2, 5, 15, or 30.
         ******************************************************************************/
        public XDocument SendDocument(string guid, string subject, Dictionary<string, RoleUser> roles, MergeField[] mergeFields, Dictionary<string, string> tags, string description, string callbackURL, int? expires_in)
        {
            string urlPath = "/api/templates.xml";
            string requestPath;
            List<string> queryParams = new List<string>();
            XElement rootNode = new XElement("template");
            XDocument xml = new XDocument(rootNode);

            // Creates the xml body to send to API
            rootNode.Add(new XElement("guid", guid));
            rootNode.Add(new XElement("subject", subject));
            rootNode.Add(new XElement("action", "send"));               // Action can be 'send' or 'prefill' 
            if (description != null)
                rootNode.Add(new XElement("description", description));
            if (expires_in != null)
                rootNode.Add(new XElement("expires_in", expires_in.ToString()));        // Must be 2, 5, 15, or 30. Otherwise, API will default it to 30 days
    
            // Create Roles XML
            XElement rolesNode = new XElement("roles");
            foreach (KeyValuePair<string, RoleUser> role in roles) {
                XElement roleNode = new XElement("role");
                roleNode.SetAttributeValue("role_name", role.Key);
                roleNode.Add(new XElement("name", role.Value.name));
                roleNode.Add(new XElement("email", role.Value.email));
                roleNode.Add(new XElement("locked", role.Value.locked.ToString().ToLower()));
                rolesNode.Add(roleNode);
            }
            rootNode.Add(rolesNode);

            // Create mergefields XML
            if (mergeFields != null)
            {
                XElement mfsNode = new XElement("merge_fields");
                foreach (MergeField mergeField in mergeFields)
                {
                    XElement mfNode = new XElement("merge_field");
                    mfNode.SetAttributeValue("merge_field_name", mergeField.name);
                    mfNode.Add(new XElement("value", mergeField.value));
                    mfNode.Add(new XElement("locked", mergeField.locked.ToString().ToLower()));
                    mfsNode.Add(mfNode);
                }
                rootNode.Add(mfsNode);
            }

            if (tags != null)
                rootNode.Add(CreateTagsXML(tags));
            if (callbackURL != null)
                rootNode.Add(new XElement("callback_location", callbackURL));

            RightSignature.RightSignatureAPI.log("Generated xml:\n~~~~~~~~~~~~\n" + xml.ToString() + "\n~~~~~~~~~~~~\n");

            // Creates URL path with query parameters in it
            requestPath = CreateRequestPath(urlPath, queryParams);

            // Creates HTTP Request and parses it as XDocument
            return ParseResponseAsXML(HttpRequest(requestPath, "POST", xml.ToString()));
        }

        /******************************************************************************
         * STATIC FUNCTIONS
        ******************************************************************************/
        private static void log(string message) {
            if (debug)
                Console.WriteLine(message);
        }



        /******************************************************************************
         * PRIVATE FUNCTIONS
         ******************************************************************************/

        // Converts a List and request path into one request path with paramters
        private string CreateRequestPath(string path, List<string> parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                path += i == 0 ? "?" : "&";
                path += parameters[i];
            }
            return path;
        }

        // Converts Dictionary into tags parameter
        private string CreateTagsParameter(Dictionary<string,string> tags) {
            string tagsParam = "tags=";
            int i = 0;
            foreach (KeyValuePair<string, string> tag in tags)
            {
                if (i > 0)
                    tagsParam += ',';
                if (tag.Value == null)
                    tagsParam += tag.Key;
                else
                    tagsParam += tag.Key + ":" + tag.Value;
                i++;
            }
            return tagsParam;
        }

        // Converts Dictionary into tags XML Element
        private XElement CreateTagsXML(Dictionary<string, string> tags)
        {
            XElement tagsNode = new XElement("tags");
            foreach (KeyValuePair<string, string> tag in tags)
            {
                XElement tagNode = new XElement("tag");
                tagNode.Add(new XElement("name", tag.Key));
                if (tag.Value != null)
                    tagNode.Add(new XElement("value", tag.Value));
                tagsNode.Add(tagNode);
            }
            return tagsNode;
        }

        private XDocument ParseResponseAsXML(HttpWebResponse response) {
            XmlReader xmlReader = XmlReader.Create(response.GetResponseStream());
            XDocument xdoc = XDocument.Load(xmlReader);
            xmlReader.Close();
            return xdoc;
        }
        
        private string ParseResponseAsString(HttpWebResponse response) {
            Stream resStream;
            string tempString = null;
            int count = 0;

            RightSignatureAPI.log("Reading stream");
            // we will read data via the response stream
            resStream = response.GetResponseStream();
            
            // fill the buffer with data
            count = resStream.Read(buf, 0, buf.Length);
            RightSignatureAPI.log("got " + count + " bytes of data");

            do {
                // read buffer
                count = resStream.Read(buf, 0, buf.Length);

                // make sure we read some data
                if (count != 0)
                {
                    // translate from bytes to ASCII text
                    tempString = Encoding.UTF8.GetString(buf, 0, count);

                    // continue building the string
                    sb.Append(tempString);
                }
            } while (count > 0); // any more data to read?

            return sb.ToString();

        }


        /******************************************************************************
         * Sends given path, method, and body to API and returns response.
         * path - URL path
         *  ex. "/api/documents.xml"
         * method - HTTP method
         *  ex. "GET"
         * body - Request Body as string
         *  ex. "<document><guid>ZNMSDFLK1JBFD</guid></document>"
         ******************************************************************************/
        private HttpWebResponse HttpRequest(string path, string method, string body) {
            // Creates Request
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(baseUrl + path);
            HttpWebResponse response;

            // Adds Secure Token to Header
            request.Headers.Add("api-token", apiToken);
            request.Method = method;

            if (method.Equals("POST")) {
                request.ContentType = "text/xml;charset=utf-8";
                if (body != null) {
                    request.ContentLength = body.Length;
                    // Writes data to request
                    using (Stream writeStream = request.GetRequestStream()) {
                        UTF8Encoding encoding = new UTF8Encoding();
                        byte[] bytes = encoding.GetBytes(body);
                        writeStream.Write(bytes, 0, bytes.Length);
                    }
                }

            }

            
            // execute the request
            RightSignatureAPI.log("Getting Request");

            response = (HttpWebResponse)request.GetResponse();

            return response;
        }
    }
}
