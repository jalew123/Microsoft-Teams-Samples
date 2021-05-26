﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCatalogSample.Dialogs;
using AppCatalogSample.Helper;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace AppCatalogSample.Bots
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : TeamsActivityHandler where T : Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Dialog Dialog;
        protected readonly ILogger Logger;
        protected readonly BotState UserState;
        public static List<CardData> taskInfoData = new List<CardData>();

        public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            Logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");
            string text = turnContext.Activity.Text;
            string data = String.Empty;
            IList<TeamsApp> teamsApps = null;
            Microsoft.Bot.Schema.Attachment attachData = null;
            string logintext = "User is not login.Type 'login' to proceed";
            AppCatalogHelper appCatalog = new AppCatalogHelper();
            switch (text)
            {
                case "login":
                case "logout":
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                    break;
                case "listapp":
                    teamsApps = appCatalog.GetAllapp().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (teamsApps != null && teamsApps.Count > 0)
                    {
                        taskInfoData = appCatalog.ParseData(teamsApps);
                        attachData = appCatalog.AgendaAdaptiveList("listapp", taskInfoData);
                        taskInfoData.Clear();
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachData), cancellationToken);
                        await appCatalog.SendListActionAsync(turnContext, cancellationToken);
                    }
                    else
                    {
                        //login if not authenticated
                        await turnContext.SendActivityAsync(MessageFactory.Text(logintext, logintext), cancellationToken);
                    }

                    break;
                case "app":
                    teamsApps = appCatalog.AppCatalogById().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (teamsApps != null && teamsApps.Count > 0)
                    {
                        taskInfoData = appCatalog.ParseData(teamsApps);
                        attachData = appCatalog.AgendaAdaptiveList("App", taskInfoData);
                        taskInfoData.Clear();
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachData), cancellationToken);
                        await appCatalog.SendListActionAsync(turnContext, cancellationToken);
                    }
                    else
                    {
                        //login if not authenticated
                        await turnContext.SendActivityAsync(MessageFactory.Text(logintext, logintext), cancellationToken);
                    }

                    break;
                case "findapp":
                    teamsApps = appCatalog.FindApplicationByTeamsId().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (teamsApps != null && teamsApps.Count > 0)
                    {
                        taskInfoData = appCatalog.ParseData(teamsApps);
                        attachData = appCatalog.AgendaAdaptiveList("findapp", taskInfoData);
                        taskInfoData.Clear();
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachData), cancellationToken);
                        await appCatalog.SendListActionAsync(turnContext, cancellationToken);
                    }
                    else
                    {
                        //login if not authenticated
                        await turnContext.SendActivityAsync(MessageFactory.Text(logintext, logintext), cancellationToken);
                    }

                    break;
                case "status":
                    teamsApps = appCatalog.AppStatus().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (teamsApps != null && teamsApps.Count > 0)
                    {
                        taskInfoData = appCatalog.ParseData(teamsApps);
                        attachData = appCatalog.AgendaAdaptiveList("status", taskInfoData);
                        taskInfoData.Clear();
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachData), cancellationToken);
                        await appCatalog.SendListActionAsync(turnContext, cancellationToken);
                    }
                    else
                    {
                        //login if not authenticated
                        await turnContext.SendActivityAsync(MessageFactory.Text(logintext, logintext), cancellationToken);
                    }

                    break;
                case "bot":
                    teamsApps = appCatalog.ListAppHavingBot().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (teamsApps != null && teamsApps.Count > 0)
                    {
                        taskInfoData = appCatalog.ParseData(teamsApps);
                        attachData = appCatalog.AgendaAdaptiveList("bot", taskInfoData);
                        taskInfoData.Clear();
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachData), cancellationToken);
                        await appCatalog.SendListActionAsync(turnContext, cancellationToken);
                    }
                    else
                    {
                        //login if not authenticated
                        await turnContext.SendActivityAsync(MessageFactory.Text(logintext, logintext), cancellationToken);
                    }

                    break;
                case "update":
                    var upData = appCatalog.UpdateFileAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (upData == "require login")
                        await turnContext.SendActivityAsync(MessageFactory.Text(logintext, logintext), cancellationToken);
                    else
                    {
                        attachData = appCatalog.AdaptivCardList("Update", upData);
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachData), cancellationToken);
                        await AppCatalogHelper.SendSuggestedActionsAsync(turnContext, cancellationToken);
                    }
                    break;
                case "publish":
                    var pubData = appCatalog.UploadFileAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (pubData == "require login")
                        await turnContext.SendActivityAsync(MessageFactory.Text(logintext, logintext), cancellationToken);
                    else
                    {
                        attachData = appCatalog.AdaptivCardList("publish", pubData);
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(attachData), cancellationToken);
                        await AppCatalogHelper.SendSuggestedActionsAsync(turnContext, cancellationToken);
                    }
                    break;

                case "delete":
                    var delData = appCatalog.DeleteApp().ConfigureAwait(false).GetAwaiter().GetResult();
                    if (String.IsNullOrEmpty(delData))
                        await turnContext.SendActivityAsync(MessageFactory.Text(logintext, logintext), cancellationToken);
                    else
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text("Delete app successfully"), cancellationToken);
                        await AppCatalogHelper.SendSuggestedActionsAsync(turnContext, cancellationToken);
                    }
                    break;
                case "list":
                    await appCatalog.SendListActionAsync(turnContext, cancellationToken);
                    break;
                case "home":
                    await AppCatalogHelper.SendSuggestedActionsAsync(turnContext, cancellationToken);
                    break;
                default:
                    await turnContext.SendActivityAsync(MessageFactory.Text(text, text), cancellationToken);
                    break;
                    // Run the Dialog with the new message Activity.
                    // await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
            }
        }


        


        
    }
}