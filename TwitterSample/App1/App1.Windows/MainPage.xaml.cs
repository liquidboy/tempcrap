using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace App1
{
    //Twitter API calls
    //https://linqtotwitter.codeplex.com/wikipage?title=Searching Twitter


    //Hyperlink Selected
    //http://www.jonathanantoine.com/2013/05/30/win8xaml-how-to-create-a-textblock-with-clickables-hyperlinks-in-it/

    public sealed partial class MainPage : Page
    {

        public ObservableCollection<Tweet> Tweets { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            Tweets = new ObservableCollection<Tweet>();
            
        }



        //async Task<HttpResponseMessage> GetToken(string key, string secret)
        //{
        //    var oAuthConsumerKey = key;
        //    var oAuthConsumerSecret = secret;
        //    var oAuthUri = new Uri("https://api.twitter.com/oauth2/token");

        //    HttpClient httpClient = new HttpClient();
        //    var authHeader = string.Format("Basic {0}",
        //    Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.EscapeDataString(oAuthConsumerKey) + ":" +
        //    Uri.EscapeDataString(oAuthConsumerSecret))));
        //    httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);

        //    HttpRequestMessage msg = new HttpRequestMessage(new HttpMethod("POST"), new Uri("https://api.twitter.com/oauth2/token"));
        //    msg.Content = new StringContent("grant_type=client_credentials");
        //    msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        //    HttpResponseMessage response = await httpClient.SendAsync(msg);
        //    return response;
        //}


        private async void butSearch_Click(object sender, RoutedEventArgs e)
        {
            await DoSearchAsync(tbSearch.Text, 100);
        }

        private async Task DoSearchAsync(string query, byte count = 25)
        {
            //var response = await GetToken("[Get from twitter App area]", "[Get from twitter App area]");
            //var msg = response.Content;

            Tweets.Clear();
            wvStream.NavigateToString("");

            var auth = new SingleUserAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = "K2ZYKTBGmii2ecC9yTaEmw",
                    ConsumerSecret = "fRHF3wJ068VO39s6Y3lF03IndmrgwWyB3EZadVXjo",
                    AccessToken = "70922793-Ca3LeHDWFaJes4Hdh9UQ5vhGKPKOizXgbhMy2zh5P",
                    AccessTokenSecret = "GXgljVMH1WB5w52J2u5tipM5XezXp24F7f5EI6TNuf2vO"
                }
            };


            var twitterCtx = new TwitterContext(auth);


            var searchResponse =
                await
                (from search in twitterCtx.Search
                 where search.Type == SearchType.Search &&
                       search.Query == "\"" + query + "\"" && search.Count == count
                 select search)
                .SingleOrDefaultAsync();
            

            if (searchResponse != null && searchResponse.Statuses != null)
                foreach (var tweet in searchResponse.Statuses)
                {

                    var newTweet = new Tweet()
                    {
                        Name = tweet.User.Name,
                        NameAt = "@" + tweet.User.ScreenNameResponse,
                        TextRaw = tweet.Text,
                        TextHtml = ParseForHtml(tweet.Text),
                        TextDateTime = tweet.CreatedAt,
                        AvatarUrl = tweet.User.ProfileImageUrl
                    };
                    
                    
                    Tweets.Add(newTweet);
                    
                }


            //using binding to populate webview UI
            HtmlBindingHelper.SetTag(wvStream, Tweets.ToList<Tweet>());
        }

        


        private string ParseForHtml(string message)
        {
            var ret = message;

            //urls
            Regex urlRx = new Regex(@"(http|ftp|https)://([\w+?\.\w+])+([a-zA-Z0-9\~\!\@\#\$\%\^\&\*\(\)_\-\=\+\\\/\?\.\:\;\'\,]*)?", RegexOptions.IgnoreCase);
            MatchCollection matches = urlRx.Matches(message);
            foreach (Match match in matches)
            {
                ret = ret.Replace(match.Value, string.Format("<span class='url1' onclick='window.external.notify(\"url|{0}\")'>{0}</span>", cleanString(match.Value, "|")));
            }


            //@
            Regex atRx = new Regex(@"(@)([a-zA-Z0-9\~\!\@\#\$\%\^\&\*\(\)_\-\=\+\\\/\?\.\:\;\'\,]*)?", RegexOptions.IgnoreCase);
            MatchCollection matchesAt = atRx.Matches(ret);
            foreach (Match match in matchesAt)
            {
                ret = ret.Replace(match.Value, string.Format("<span class='at1' onclick='window.external.notify(\"at|{0}\")'>{0}</span>", cleanString(match.Value, "|")));
            }

            //#
            Regex hashRx = new Regex(@"(#)([a-zA-Z0-9\~\!\@\#\$\%\^\&\*\(\)_\-\=\+\\\/\?\.\:\;\'\,]*)?", RegexOptions.IgnoreCase);
            MatchCollection matchesHash = hashRx.Matches(ret);
            foreach (Match match in matchesHash)
            {
                ret = ret.Replace(match.Value, string.Format("<span class='hsh1' onclick='window.external.notify(\"hash|{0}\")'>{0}</span>", cleanString(match.Value, "|")));
            }


            return ret;
        }

        


        private string cleanString(string raw , string charToRemove)
        {
            return raw.Replace(charToRemove, "'");
        }







        private void rtbText_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var richTB = sender as RichTextBlock;
            var textPointer = richTB.GetPositionFromPoint(e.GetPosition(richTB));

            var element = textPointer.Parent as TextElement;
            
            if(element is Run)
            {
                var run = (Run)element;
                if (run.Text.StartsWith("@"))
                {

                }else if (run.Text.StartsWith("#"))
                {

                }
                if (run.Text.StartsWith("http://") || run.Text.StartsWith("https://"))
                {

                }
            }

            
        }

        private void wvStream_ScriptNotify(object sender, NotifyEventArgs e)
        {
            var yes = e.Value;
            var parts = yes.Split("|".ToCharArray());

            if (parts[0] == "at")
            {

            }
            else if (parts[0] == "hash")
            {

            }
            else if (parts[0] == "url")
            {

            }

        }
    }

    public class HtmlBindingHelper
    {
        //note: if this were ObservableCollection<tweet> it would not recieve updates as the collection is 
        //the same guid between setTags .. hence why i made it a "List"
        public static List<Tweet> GetTag(DependencyObject obj)
        {
            return (List<Tweet>)obj.GetValue(TagProperty);
        }

        public static void SetTag(DependencyObject obj, List<Tweet> value)
        {
            obj.SetValue(TagProperty, value);
        }

        public static readonly DependencyProperty TagProperty =
            DependencyProperty.RegisterAttached("Tag", typeof(string), typeof(HtmlBindingHelper),
                new PropertyMetadata(new List<Tweet>(), OnTagChanged));

        private static void OnTagChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var wv = (WebView)sender;
            if (e!=null && e.NewValue != null && e.NewValue is List<Tweet>)
            {
                var tweets = (List<Tweet>)e.NewValue;

                var htmlStream = "";
                var htmlStyle = "<style text='text/css' > "
                 + "body{ font-family:segoe ui, arial; font-size:16px;} "
                 + ".tw{ min-height:80px;float:left; margin-bottom:10px;} "
                 + ".tw .av{float:left;width:50px;margin-top:5px;} "
                 + ".tw .twtxt{float:right; width:410px;margin-left:10px;} "
                 + ".tw .u1{font-weight:bold;} "
                 + ".tw .u2{color:grey;margin-left:10px;} "
                 + ".tw .url1{color:blue;text-decoration:underline;cursor:pointer;} "
                 + ".tw .at1{color:green;text-decoration:italic;cursor:pointer;} "
                 + ".tw .hsh1{color:orange;text-decoration:italic;cursor:pointer;} "
                 + ".annoying{opacity:0.3;} "
                 + "</style>";

                foreach (var tweet in tweets)
                {
                    var specialCssClasses = "";
                    if (tweet.NameAt.ToLower() == "@virtuame") specialCssClasses += " annoying";

                    var htmlTemplate = ""
                        + "<div class='tw {4}'>"
                        + " <img src='{0}' class='av'  />"
                        + " <div class='twtxt'>"
                        + "     <span class='u1'>{1}</span>"
                        + "     <span class='u2'>{2}</span>"
                        + "     <div>{3}</div>"
                        + " </div>"
                        + "</div>"
                        ;
                    htmlStream += string.Format(htmlTemplate, tweet.AvatarUrl, tweet.Name, tweet.NameAt, tweet.TextHtml, specialCssClasses);

                }

                wv.NavigateToString(htmlStyle + htmlStream);
            }
            
        }
    }

    public class RichTextBindingHelper : DependencyObject
    {
        public static string GetText(DependencyObject obj)
        {
            return (string)obj.GetValue(TextProperty);
        }

        public static void SetText(DependencyObject obj, string value)
        {
            obj.SetValue(TextProperty, value);
        }
        
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(RichTextBindingHelper), 
                new PropertyMetadata(String.Empty, OnTextChanged));

        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as RichTextBlock;
            if (control != null)
            {
                control.Blocks.Clear();
                string value = e.NewValue.ToString();

                
                var paragraph = ParseForRichTextParagrah(value);
                control.Blocks.Add(paragraph);
            }
        }

        static Windows.UI.Xaml.Media.SolidColorBrush greenBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
        static Windows.UI.Xaml.Media.SolidColorBrush orangeBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Orange);
        static Windows.UI.Xaml.Media.SolidColorBrush blueBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Blue);


        private static Paragraph ParseForRichTextParagrah(string message)
        {

            var ret = new Paragraph();

            //ret.Inlines.Add(new Run { Text = message });

            
            var words = message.Split(" ".ToCharArray());
            foreach (var word in words)
            {
                if(word.Contains("@"))
                    ret.Inlines.Add(new Run { Text = word + " ",  Foreground = greenBrush });
                else if (word.StartsWith("#"))
                    ret.Inlines.Add(new Run { Text = word + " ",  Foreground = orangeBrush });
                else if (word.StartsWith("http://") || word.StartsWith("https://"))
                {
                    var ul = new Underline();
                    ul.Inlines.Add(new Run { Text = word + " ", Foreground = blueBrush });
                    ret.Inlines.Add(ul);

                }
                else
                    ret.Inlines.Add(new Run { Text = word + " " });
            }

            return ret;
        }
    }


    public class Tweet
    {
        public string Name { get; set; }
        public string TextHtml { get; set; }
        public string TextRaw { get; set; }
        public DateTime TextDateTime { get; set; }
        public string AvatarUrl { get; set; }
        public string NameAt { get; set; }

    }
}
