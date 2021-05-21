using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards.Templating;
using AdaptiveCards;
using Microsoft.Bot.Schema.Teams;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Builder.Teams;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Bot.Builder.Dialogs;

namespace EchoBot.Bots
{
    public class EchoBot : TeamsActivityHandler
    {
        private readonly IHttpClientFactory _clientFactory;
        private static string microsoftAppId;
        private static string microsoftAppPassword;
        public EchoBot(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            microsoftAppId = "e5e30201-b5fb-4689-bfd6-7f7369cb2eca";
            microsoftAppPassword = "t5tMARxJb~xmUU3HOKJ9-16Cr9wolR-k-H";
        }

        protected override async Task OnTeamsFileConsentAcceptAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            try
            {
                JToken context = JObject.FromObject(fileConsentCardResponse.Context);

                string filePath = Path.Combine("Files", context["filename"].ToString());
                long fileSize = new FileInfo(filePath).Length;
                var client = _clientFactory.CreateClient();
                using (var fileStream = File.OpenRead(filePath))
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentLength = fileSize;
                    fileContent.Headers.ContentRange = new ContentRangeHeaderValue(0, fileSize - 1, fileSize);
                    await client.PutAsync(fileConsentCardResponse.UploadInfo.UploadUrl, fileContent, cancellationToken);
                }

                await FileUploadCompletedAsync(turnContext, fileConsentCardResponse, cancellationToken);
            }
            catch (Exception e)
            {
                await FileUploadFailedAsync(turnContext, e.ToString(), cancellationToken);
            }
        }

        protected override async Task OnTeamsFileConsentDeclineAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            JToken context = JObject.FromObject(fileConsentCardResponse.Context);

            var reply = MessageFactory.Text($"Declined. We won't upload file <b>{context["filename"]}</b>.");
            reply.TextFormat = "xml";
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private async Task FileUploadCompletedAsync(ITurnContext turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            var downloadCard = new FileInfoCard
            {
                UniqueId = fileConsentCardResponse.UploadInfo.UniqueId,
                FileType = fileConsentCardResponse.UploadInfo.FileType,
            };

            var asAttachment = new Attachment
            {
                Content = downloadCard,
                ContentType = FileInfoCard.ContentType,
                Name = fileConsentCardResponse.UploadInfo.Name,
                ContentUrl = fileConsentCardResponse.UploadInfo.ContentUrl,
            };

            var reply = MessageFactory.Text($"<b>File uploaded.</b> Your file <b>{fileConsentCardResponse.UploadInfo.Name}</b> is ready to download");
            reply.TextFormat = "xml";
            reply.Attachments = new List<Attachment> { asAttachment };

            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private async Task FileUploadFailedAsync(ITurnContext turnContext, string error, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text($"<b>File upload failed.</b> Error: <pre>{error}</pre>");
            reply.TextFormat = "xml";
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private Activity CreateResponse(Activity activity, Attachment attachment)
        {

            var response = activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        private Attachment CreateAdaptiveCardAttachment(string cart)
        {
            var adaptiveCard = File.ReadAllText(cart);
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {

            dynamic Value = turnContext.Activity.Value;
            if (Value != null)
            {
                if(Value.type == "fileUpload")
                {
                    await base.OnTurnAsync(turnContext, cancellationToken);
                    return;
                }
                if (Value.id.ToString() == "1234567890")
                {
                    var adaptiveCard = File.ReadAllText(@".\AdaptiveCardUpdate.json");
                    var newString = adaptiveCard.Insert(245, Value.MultiLineVal.ToString());
                    var newString2 = newString.Insert(172, Value.CompactSelectVal.ToString());

                    var newActivity = CreateResponse(turnContext.Activity, new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(newString2),
                    });
                    newActivity.Id = turnContext.Activity.ReplyToId;

                    await turnContext.UpdateActivityAsync(newActivity, cancellationToken);
                    return;
                }
                if (Value.id.ToString() == "76646a55-919a-473a-bdae-7e2ba22a8394")
                {
                    var adaptiveCard = File.ReadAllText(@".\DialogTemplateSecond.json");
                    var newActivity = CreateResponse(turnContext.Activity, new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    });
                    newActivity.Id = turnContext.Activity.ReplyToId;

                    await turnContext.UpdateActivityAsync(newActivity, cancellationToken);
                    return;
                }
                if (Value.id.ToString() == "e9eff60f-485b-4a06-bc67-d936193eee8a")
                {
                    var adaptiveCard = File.ReadAllText(@".\DialogTemplateSecondUpdate.json");
                    var newString = adaptiveCard.Insert(604, Value.MultiLineVal.ToString());
                    var newString2 = newString.Insert(487, Value.CompactSelectVal.ToString());
                    var newString3 = newString2.Insert(371, Value.SimpleVal.ToString());
                    var newActivity = CreateResponse(turnContext.Activity, new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(newString3),
                    });
                    newActivity.Id = turnContext.Activity.ReplyToId;

                    await turnContext.UpdateActivityAsync(newActivity, cancellationToken);
                    await turnContext.SendActivityAsync(CreateResponse(turnContext.Activity as Activity, CreateAdaptiveCardAttachment(@".\DialogTemplateLast.json")));
                    return;
                }
            }
                await base.OnTurnAsync(turnContext, cancellationToken);


            
        }

        private async Task SendFileCardAsync(ITurnContext turnContext, string filename, long filesize, CancellationToken cancellationToken)
        {
            var consentContext = new Dictionary<string, string>
            {
                { "filename", filename },
            };

            var fileCard = new FileConsentCard
            {
                Description = "This is the file I want to send you",
                SizeInBytes = filesize,
                AcceptContext = consentContext,
                DeclineContext = consentContext,
            };

            var asAttachment = new Attachment
            {
                Content = fileCard,
                ContentType = FileConsentCard.ContentType,
                Name = filename,
            };

            var replyActivity = turnContext.Activity.CreateReply();
            replyActivity.Attachments = new List<Attachment>() { asAttachment };
            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }



        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            bool messageWithFileDownloadInfo = turnContext.Activity.Attachments?[0].ContentType == FileDownloadInfo.ContentType;
            if (messageWithFileDownloadInfo)
            {
                var file = turnContext.Activity.Attachments[0];
                string filePath = Path.Combine("Files", file.Name);     
                await SendFileCardAsync(turnContext, file.Name, new FileInfo(filePath).Length, cancellationToken);
                return;
            }

            var replyText = $"Echo: {turnContext.Activity.Text}";
            if (turnContext.Activity.Text == "Mood " || turnContext.Activity.Text == "Mood")
            {
                await turnContext.SendActivityAsync(CreateResponse(turnContext.Activity as Activity, CreateAdaptiveCardAttachment(@".\AdaptiveCard.json")));

                return;
            }

            if (turnContext.Activity.Text == "Newcomer survey " || turnContext.Activity.Text == "Newcomer survey")
            {
                var adaptiveCard = File.ReadAllText(@".\DialogTemplateFirst.json");
                var newString = adaptiveCard.Insert(209, turnContext.Activity.From.Name);
                var newActivity = new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(newString),
                };
                await turnContext.SendActivityAsync(CreateResponse(turnContext.Activity as Activity, newActivity));
                return;
            }
           
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);


        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }

}
