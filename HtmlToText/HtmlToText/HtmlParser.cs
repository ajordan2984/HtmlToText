using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace HtmlToText
{
    public class HtmlParser
    {
        #region Private Members
        private StringBuilder CurrentBuilder;
        private Dictionary<string, StringBuilder> TextBuilder;
        private Dictionary<string, List<string>> Results;

        // Html helper
        private readonly List<string> HtmlTagsWithText;
        #endregion

        #region Constructors
        public HtmlParser()
        {
            #region Private Members Initialization
            AllExceptions = new List<Exception>();
            CurrentBuilder = null;

            TextBuilder = new Dictionary<string, StringBuilder>
            {
                { "div", new StringBuilder() },
                { "p", new StringBuilder() },
                { "h1", new StringBuilder() },
                { "h2", new StringBuilder() },
                { "h3", new StringBuilder() },
                { "h4", new StringBuilder() },
                { "h5", new StringBuilder() },
                { "h6", new StringBuilder() }
            };

            Results = new Dictionary<string, List<string>>
            {
                { "div", new List<string>() },
                { "p", new List<string>() },
                { "h1", new List<string>() },
                { "h2", new List<string>() },
                { "h3", new List<string>() },
                { "h4", new List<string>() },
                { "h5", new List<string>() },
                { "h6", new List<string>() },
                {"ApplicationLdJson", new List<string>() }
            };

            HtmlTagsWithText = new List<string>
            {
                // Containers
                "span","article","section","blockquote","a",
                // Format
                "b","strong","i","em","mark","small","del","ins","sub","sup","center","font","tt","pre","s","dfn","li","dt","dd"
            };
            #endregion

            #region Public Members Initialization
            AllExceptions = new List<Exception>();

            MetaFinal = new Dictionary<string, string>
            {
                { "og:site_name", "" },
                { "og:url", "" },
                { "og:title", "" },
                { "og:description", "" },
                { "og:image", "" },
                { "og:image:alt", "" },
                { "article:author", "" },
                { "article:section", "" },
                { "article:tag", "" },
                { "article:published_time", "" },
                { "article:modified_time", "" }
            };
            #endregion
        }
        #endregion

        #region Public Properties
        public List<string> Div
        {
            get => Results["div"];
            set => Results["div"] = value;
        }

        public List<string> Paragraph
        {
            get => Results["p"];
            set => Results["p"] = value;
        }

        public List<string> H1
        {
            get => Results["h1"];
            set => Results["h1"] = value;
        }

        public List<string> H2
        {
            get => Results["h2"];
            set => Results["h2"] = value;
        }

        public List<string> H3
        {
            get => Results["h3"];
            set => Results["h3"] = value;
        }

        public List<string> H4
        {
            get => Results["h4"];
            set => Results["h4"] = value;
        }

        public List<string> H5
        {
            get => Results["h5"];
            set => Results["h5"] = value;
        }

        public List<string> H6
        {
            get => Results["h6"];
            set => Results["h6"] = value;
        }

        public List<string> ApplicationLdJson
        {
            get => Results["ApplicationLdJson"];
            set => Results["ApplicationLdJson"] = value;
        }

        public List<Exception> AllExceptions { get; set; }

        public Dictionary<string, string> MetaFinal { get; set; }
        #endregion

        #region Public Methods
        public void ParseUrl(string url)
        {
            ResetCollections();

            string htmlDocument = DownloadHtml(url);

            if (!string.IsNullOrEmpty(htmlDocument))
            {
                int i = 0;

                try
                {
                    while (i < htmlDocument.Length)
                    {
                        ProcessTag(htmlDocument, ref i, htmlDocument.Length);
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    AllExceptions.Add(ex);
                }
            }
        }
        #endregion

        #region Private Methods
        private void ResetCollections()
        {
            AllExceptions.Clear();
            CurrentBuilder = null;

            foreach (var builder in TextBuilder.Values)
            {
                builder.Clear();
            }

            foreach (var list in Results.Values)
            {
                list.Clear();
            }

            foreach (var key in MetaFinal.Keys.ToList())
            {
                MetaFinal[key] = "";
            }
        }
        
        private string DownloadHtml(string url)
        {
            try
            {
                var Client = new HttpClient(new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.All
                });

                // ### Client Headers to simulate a browser loading the page ###
                Client.DefaultRequestHeaders.Add("user-agent", @"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                Client.DefaultRequestHeaders.Add("Accept-Language", @"en-US,en;q=0.9");
                Client.DefaultRequestHeaders.Add("Accept-Encoding", @"gzip, deflate, br");
                Client.DefaultRequestHeaders.Add("Accept", @"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                Client.DefaultRequestHeaders.Add("referrer", @"http://www.google.com/");
                Client.DefaultRequestHeaders.Add("Pragma", @"no-cache");

                var response = Client.GetAsync(url).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                AllExceptions.Add(ex);
            }
            return null;
        }

        private string BuildTag(string doc, ref int i, int end)
        {
            StringBuilder buildHtmlTag = new StringBuilder();

            // ### Skip past the '<' in the tag ###
            i++;

            // ### Build the html tag ###
            while (i < end && doc[i] != ' ' && doc[i] != '>')
            {
                if (!char.IsWhiteSpace(doc[i]))
                {
                    buildHtmlTag.Append(doc[i]);
                }

                i++;
            }

            return buildHtmlTag.ToString().ToLower();
        }

        private void ChangeCurrentBuilder(string tag)
        {
            if (TextBuilder.ContainsKey(tag))
            {
                CurrentBuilder = TextBuilder[tag];
                return;
            }

            if (CurrentBuilder == null && (tag == "span" || tag == "article" || tag == "section" || tag == "blockquote"))
            {
                CurrentBuilder = TextBuilder["div"];
                return;
            }
        }

        private void ProcessTag(string doc, ref int i, int end)
        {
            try
            {
                if (doc[i] == '<')
                {
                    string tag = SpecialCaseTag(doc, ref i, end, BuildTag(doc, ref i, end));

                    // ### OPENING TAG ###
                    if (!string.IsNullOrEmpty(tag))
                    {
                        ChangeCurrentBuilder(tag);
                        bool appendToCurrentBuilder = HtmlTagsWithText.Contains(tag) || Results.ContainsKey(tag);
                        bool exitFlag = false;

                        while (i < end && !exitFlag)
                        {
                            if (doc[i] == '<') // ### Possible opening or closing tag was found ###
                            {
                                exitFlag = doc[i + 1] == '/'; // ### All nested tags processed - exit loop ###
                                ProcessTag(doc, ref i, end); // ### Start of html tag found ###
                            }
                            else
                            {
                                if (CurrentBuilder != null && appendToCurrentBuilder && doc[i] != '>')
                                {
                                    CurrentBuilder.Append(doc[i]);
                                }
                                i++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AllExceptions.Add(ex);
            }
        }

        private string SpecialCaseTag(string doc, ref int i, int end, string builtHtmlTag)
        {
            try
            {
                // ### NON HTML TAG ###
                if (string.IsNullOrWhiteSpace(builtHtmlTag))
                {
                    return null;
                }

                if (builtHtmlTag[0] == '!')
                {
                    char sequence = '-';
                    bool longSq = false;

                    // ### HTML COMMENT TAG ###
                    // Sample tags:
                    //  <!-->
                    //  <!---->
                    //  <!-- <>This is a comment<> -->
                    //  <!-- This is a comment -->
                    //  <!-- >This is a comment-->
                    //  <!--This is a comment> --> 
                    if (builtHtmlTag.Length > 2 && builtHtmlTag[1] == '-' && builtHtmlTag[2] == '-')
                    {
                        longSq = true;
                    }

                    // ### <![CDATA[...]]> TAG ###
                    if (builtHtmlTag.Length > 1 && builtHtmlTag[1] == '[' && builtHtmlTag[2] == 'c')
                    {
                        longSq = true;
                        sequence = ']';
                    }

                    if (longSq)
                    {
                        while (doc[i - 2] != sequence || doc[i - 1] != sequence || doc[i] != '>')
                        {
                            i++;
                        }
                        return null;
                    }
                }

                // ### SCRIPT TAG ###
                if (builtHtmlTag == "script")
                {
                    ParseApplicationLdJson(doc, ref i, end);
                    return null;
                }

                // ### META TAG ###
                if (builtHtmlTag == "meta")
                {
                    ParseMetaTag(doc, ref i);
                    return null;
                }

                // ### Skip past tag ###
                while (doc[i] != '>')
                {
                    i++;
                }

                // ### CLOSING TAG ###
                if (builtHtmlTag[0] == '/')
                {
                    string closingTag = builtHtmlTag.Substring(1);

                    if (TextBuilder.ContainsKey(closingTag))
                    {
                        ChangeCurrentBuilder(closingTag);
                        ParseTextFound(closingTag);
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                AllExceptions.Add(ex);
            }
            return builtHtmlTag;
        }

        private void ParseApplicationLdJson(string doc, ref int i, int end)
        {
            try
            {
                bool canParse = false;

                // Move to the end of the first tag "<script...application/ld+json...>...</script>"
                while (doc[i - 1] != '>') // Skips past the ">" so that i is inside the tag
                {
                    if (i + 18 < end)
                    {
                        if (doc[i] == 'a' && doc[i + 1] == 'p' && doc[i + 2] == 'p' && doc[i + 3] == 'l' && doc[i + 4] == 'i' && doc[i + 5] == 'c' && doc[i + 6] == 'a' && doc[i + 7] == 't' && doc[i + 8] == 'i' && doc[i + 9] == 'o' && doc[i + 10] == 'n' &&
                             doc[i + 11] == '/' &&
                             doc[i + 12] == 'l' && doc[i + 13] == 'd' &&
                             doc[i + 14] == '+' &&
                             doc[i + 15] == 'j' && doc[i + 16] == 's' && doc[i + 17] == 'o' && doc[i + 18] == 'n')
                        {
                            canParse = true;
                        }
                    }
                    i++;
                }

                StringBuilder textInside = new StringBuilder();

                // </script>
                while (doc[i] != '<' || doc[i + 1] != '/' || doc[i + 2] != 's' || doc[i + 3] != 'c' || doc[i + 4] != 'r' || doc[i + 5] != 'i' || doc[i + 6] != 'p' || doc[i + 7] != 't' || doc[i + 8] != '>')
                {
                    if (canParse)
                    {
                        textInside.Append(doc[i]);
                    }
                    i++;
                }

                // Skip to the end of </script>
                i += 8;

                if (textInside.Length != 0)
                {
                    Results["ApplicationLdJson"].Add(textInside.ToString());
                }
            }
            catch (Exception ex)
            {
                AllExceptions.Add(ex);
            }
        }

        private void ParseMetaTag(string doc, ref int i)
        {
            string key = "";
            string value = "";

            try
            {
                while (doc[i] != '>')
                {
                    // Find content=
                    // Move index past the '='
                    if (doc[i] == 'c' && doc[i + 1] == 'o' && doc[i + 2] == 'n' && doc[i + 3] == 't' && doc[i + 4] == 'e' && doc[i + 5] == 'n' && doc[i + 6] == 't' && doc[i + 7] == '=')
                    {
                        i += 8;
                        value = ParseMetaTagSection(doc, ref i);
                    }

                    // Find property=
                    // Move index past the '='
                    if (doc[i] == 'p' && doc[i + 1] == 'r' && doc[i + 2] == 'o' && doc[i + 3] == 'p' && doc[i + 4] == 'e' && doc[i + 5] == 'r' && doc[i + 6] == 't' && doc[i + 7] == 'y' && doc[i + 8] == '=')
                    {
                        i += 9;
                        key = ParseMetaTagSection(doc, ref i);
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                AllExceptions.Add(ex);
            }

            if (!string.IsNullOrEmpty(key) && MetaFinal.ContainsKey(key))
            {
                if (key == "article:tag")
                {
                    if (string.IsNullOrEmpty(MetaFinal[key]))
                    {
                        MetaFinal[key] = value;
                    }
                    else
                    {
                        MetaFinal[key] = MetaFinal[key] + "," + value;
                    }
                }
                if (MetaFinal.ContainsKey(key))
                {
                    MetaFinal[key] = value;
                }
            }
        }

        private string ParseMetaTagSection(string doc, ref int i)
        {
            char endFlag = ' ';
            string temp = "";

            if (doc[i] == '"' || doc[i] == '\'')
            {
                endFlag = doc[i];
                i++;
            }

            while (doc[i] != endFlag)
            {
                temp += doc[i];
                i++;
            }

            return temp;
        }

        private void ParseTextFound(string closingTag)
        {
            try
            {
                string sentence = CurrentBuilder.ToString();

                if (!string.IsNullOrEmpty(sentence))
                {
                    var trimmed = HttpUtility.HtmlDecode(Regex.Replace(sentence, @"\s+", " ").Trim());

                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        if ((char.IsPunctuation(trimmed[^1]) || closingTag[0] == 'h') && !Results[closingTag].Contains(trimmed))
                        {
                            Results[closingTag].Add(trimmed);
                        }
                    }

                    CurrentBuilder.Clear(); // ### Clear out the current string builder for the next section of text ###
                }
            }
            catch (Exception ex)
            {
                AllExceptions.Add(ex);
            }
        }
        #endregion
    }
}
