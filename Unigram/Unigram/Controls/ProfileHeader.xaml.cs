﻿using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Controls.Gallery;
using Unigram.Converters;
using Unigram.ViewModels;
using Unigram.ViewModels.Chats;
using Unigram.ViewModels.Users;
using Windows.Foundation;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Point = Windows.Foundation.Point;

namespace Unigram.Controls
{
    public sealed partial class ProfileHeader : UserControl
    {
        public ProfileViewModel ViewModel => DataContext as ProfileViewModel;

        public ProfileHeader()
        {
            InitializeComponent();
            DescriptionLabel.AddHandler(ContextRequestedEvent, new TypedEventHandler<UIElement, ContextRequestedEventArgs>(About_ContextRequested), true);

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel != null && Chat != null)
            {
                SetChat(Chat);
            }
        }

        private Chat _chat;
        public Chat Chat
        {
            get => _chat;
            set => SetChat(value);
        }
        
        private void SetChat(Chat chat)
        {
            _chat = chat;

            // Perdoname madre por mi duplicated code
            if (chat == null || ViewModel == null)
            {
                return;
            }

            UpdateChat(chat);

            if (chat.Type is ChatTypePrivate privata)
            {
                var item = ViewModel.ProtoService.GetUser(privata.UserId);
                var cache = ViewModel.ProtoService.GetUserFull(privata.UserId);

                UpdateUser(chat, item, false);

                if (cache != null)
                {
                    UpdateUserFullInfo(chat, item, cache, false, false);
                }
            }
            else if (chat.Type is ChatTypeSecret secretType)
            {
                var secret = ViewModel.ProtoService.GetSecretChat(secretType.SecretChatId);
                var item = ViewModel.ProtoService.GetUser(secretType.UserId);
                var cache = ViewModel.ProtoService.GetUserFull(secretType.UserId);

                UpdateSecretChat(chat, secret);
                UpdateUser(chat, item, true);

                if (cache != null)
                {
                    UpdateUserFullInfo(chat, item, cache, true, false);
                }
            }
            else if (chat.Type is ChatTypeBasicGroup basic)
            {
                var item = ViewModel.ProtoService.GetBasicGroup(basic.BasicGroupId);
                var cache = ViewModel.ProtoService.GetBasicGroupFull(basic.BasicGroupId);

                UpdateBasicGroup(chat, item);

                if (cache != null)
                {
                    UpdateBasicGroupFullInfo(chat, item, cache);
                }
            }
            else if (chat.Type is ChatTypeSupergroup super)
            {
                var item = ViewModel.ProtoService.GetSupergroup(super.SupergroupId);
                var cache = ViewModel.ProtoService.GetSupergroupFull(super.SupergroupId);

                UpdateSupergroup(chat, item);

                if (cache != null)
                {
                    UpdateSupergroupFullInfo(chat, item, cache);
                }
            }
        }

        private async void Photo_Click(object sender, RoutedEventArgs e)
        {
            var chat = ViewModel.Chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypePrivate or ChatTypeSecret)
            {
                var user = ViewModel.ProtoService.GetUser(chat);
                if (user == null || user.ProfilePhoto == null)
                {
                    return;
                }

                var userFull = ViewModel.ProtoService.GetUserFull(user.Id);
                if (userFull?.Photo == null)
                {
                    return;
                }

                var viewModel = new UserPhotosViewModel(ViewModel.ProtoService, ViewModel.StorageService, ViewModel.Aggregator, user, userFull);
                await GalleryView.GetForCurrentView().ShowAsync(viewModel, () => Photo);
            }
            else if (chat.Type is ChatTypeBasicGroup)
            {
                var basicGroupFull = ViewModel.ProtoService.GetBasicGroupFull(chat);
                if (basicGroupFull?.Photo == null)
                {
                    return;
                }

                var viewModel = new ChatPhotosViewModel(ViewModel.ProtoService, ViewModel.StorageService, ViewModel.Aggregator, chat, basicGroupFull.Photo);
                await GalleryView.GetForCurrentView().ShowAsync(viewModel, () => Photo);
            }
            else if (chat.Type is ChatTypeSupergroup)
            {
                var supergroupFull = ViewModel.ProtoService.GetSupergroupFull(chat);
                if (supergroupFull?.Photo == null)
                {
                    return;
                }

                var viewModel = new ChatPhotosViewModel(ViewModel.ProtoService, ViewModel.StorageService, ViewModel.Aggregator, chat, supergroupFull.Photo);
                await GalleryView.GetForCurrentView().ShowAsync(viewModel, () => Photo);
            }
        }

        #region Delegate

        public void UpdateChat(Chat chat)
        {
            UpdateChatTitle(chat);
            UpdateChatPhoto(chat);

            UpdateChatNotificationSettings(chat);
        }

        public void UpdateChatTitle(Chat chat)
        {
            Title.Text = ViewModel.ProtoService.GetTitle(chat);
        }

        public void UpdateChatPhoto(Chat chat)
        {
            Photo.SetChat(ViewModel.ProtoService, chat, 140);
        }

        public void UpdateChatNotificationSettings(Chat chat)
        {
            var unmuted = ViewModel.CacheService.Notifications.GetMutedFor(chat) == 0;
            Notifications.Content = unmuted ? Strings.Resources.ChatsMute : Strings.Resources.ChatsUnmute;
            Notifications.Glyph = unmuted ? Icons.Alert : Icons.AlertOff;
        }

        public void UpdateUser(Chat chat, User user, bool secret)
        {
            Subtitle.Text = LastSeenConverter.GetLabel(user, true);

            Verified.Visibility = user.IsVerified ? Visibility.Visible : Visibility.Collapsed;

            UserPhone.Badge = PhoneNumber.Format(user.PhoneNumber);
            UserPhone.Visibility = string.IsNullOrEmpty(user.PhoneNumber) ? Visibility.Collapsed : Visibility.Visible;

            Username.Badge = $"{user.Username}";
            Username.Visibility = string.IsNullOrEmpty(user.Username) ? Visibility.Collapsed : Visibility.Visible;

            Description.Content = user.Type is UserTypeBot ? Strings.Resources.DescriptionPlaceholder : Strings.Resources.UserBio;

            if (secret)
            {
                UserStartSecret.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (user.Type is UserTypeBot ||
                    user.Id == ViewModel.CacheService.Options.MyId ||
                    LastSeenConverter.IsServiceUser(user) ||
                    LastSeenConverter.IsSupportUser(user) ||
                    user.Type is UserTypeDeleted)
                {
                    MiscPanel.Visibility = Visibility.Collapsed;
                    UserStartSecret.Visibility = Visibility.Collapsed;
                }
                else
                {
                    MiscPanel.Visibility = Visibility.Visible;
                    UserStartSecret.Visibility = Visibility.Visible;
                }

                SecretLifetime.Visibility = Visibility.Collapsed;
                SecretHashKey.Visibility = Visibility.Collapsed;
            }

            // Unused:
            Location.Visibility = Visibility.Collapsed;
            Edit.Visibility = Visibility.Collapsed;

            GroupLeave.Visibility = Visibility.Collapsed;
            GroupInvite.Visibility = Visibility.Collapsed;

            ChannelMembersPanel.Visibility = Visibility.Collapsed;
            MembersPanel.Visibility = Visibility.Collapsed;
            //Admins.Visibility = Visibility.Collapsed;
            //Banned.Visibility = Visibility.Collapsed;
            //Restricted.Visibility = Visibility.Collapsed;
            //Members.Visibility = Visibility.Collapsed;
        }

        public void UpdateUserFullInfo(Chat chat, User user, UserFullInfo fullInfo, bool secret, bool accessToken)
        {
            if (user.Type is UserTypeBot)
            {
                GetEntities(fullInfo.ShareText);
                Description.Visibility = string.IsNullOrEmpty(fullInfo.ShareText) ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                GetEntities(fullInfo.Bio);
                Description.Visibility = string.IsNullOrEmpty(fullInfo.Bio) ? Visibility.Collapsed : Visibility.Visible;
            }

            //UserCommonChats.Badge = fullInfo.GroupInCommonCount;
            //UserCommonChats.Visibility = fullInfo.GroupInCommonCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            Call.Visibility = fullInfo.CanBeCalled ? Visibility.Visible : Visibility.Collapsed;
            VideoCall.Visibility = fullInfo.CanBeCalled && fullInfo.SupportsVideoCalls ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateUserStatus(Chat chat, User user)
        {
            Subtitle.Text = LastSeenConverter.GetLabel(user, true);
        }



        public void UpdateSecretChat(Chat chat, SecretChat secretChat)
        {
            if (secretChat.State is SecretChatStateReady)
            {
                SecretLifetime.Badge = chat.MessageTtl > 0 ? Locale.FormatTtl(chat.MessageTtl) : Strings.Resources.ShortMessageLifetimeForever;
                //SecretIdenticon.Source = PlaceholderHelper.GetIdenticon(secretChat.KeyHash, 24);

                MiscPanel.Visibility = Visibility.Visible;
                SecretLifetime.Visibility = Visibility.Visible;
                SecretHashKey.Visibility = Visibility.Visible;
            }
            else
            {
                MiscPanel.Visibility = Visibility.Collapsed;
                SecretLifetime.Visibility = Visibility.Collapsed;
                SecretHashKey.Visibility = Visibility.Collapsed;
            }
        }



        public void UpdateBasicGroup(Chat chat, BasicGroup group)
        {
            Subtitle.Text = Locale.Declension("Members", group.MemberCount);

            Description.Content = Strings.Resources.DescriptionPlaceholder;

            Verified.Visibility = Visibility.Collapsed;
            UserPhone.Visibility = Visibility.Collapsed;
            Location.Visibility = Visibility.Collapsed;
            Username.Visibility = Visibility.Collapsed;

            Description.Visibility = Visibility.Collapsed;

            //UserCommonChats.Visibility = Visibility.Collapsed;
            UserStartSecret.Visibility = Visibility.Collapsed;

            MiscPanel.Visibility = Visibility.Collapsed;

            SecretLifetime.Visibility = Visibility.Collapsed;
            SecretHashKey.Visibility = Visibility.Collapsed;

            GroupLeave.Visibility = Visibility.Collapsed;

            ChannelMembersPanel.Visibility = Visibility.Collapsed;
            MembersPanel.Visibility = Visibility.Collapsed;
            //Admins.Visibility = Visibility.Collapsed;
            //Banned.Visibility = Visibility.Collapsed;
            //Restricted.Visibility = Visibility.Collapsed;
            //Members.Visibility = Visibility.Collapsed;

            GroupInvite.Visibility = group.Status is ChatMemberStatusCreator || (group.Status is ChatMemberStatusAdministrator administrator && administrator.CanInviteUsers) || chat.Permissions.CanInviteUsers ? Visibility.Visible : Visibility.Collapsed;

            Edit.Visibility = chat.Permissions.CanChangeInfo || group.Status is ChatMemberStatusCreator || group.Status is ChatMemberStatusAdministrator ? Visibility.Visible : Visibility.Collapsed;
            Edit.Glyph = Icons.Edit;
            Edit.Content = Strings.Resources.ChannelEdit;

            // Unused:
            Call.Visibility = Visibility.Collapsed;
            VideoCall.Visibility = Visibility.Collapsed;
        }

        public void UpdateBasicGroupFullInfo(Chat chat, BasicGroup group, BasicGroupFullInfo fullInfo)
        {
            GetEntities(fullInfo.Description);
            Description.Visibility = string.IsNullOrEmpty(fullInfo.Description) ? Visibility.Collapsed : Visibility.Visible;
        }



        public void UpdateSupergroup(Chat chat, Supergroup group)
        {
            Subtitle.Text = Locale.Declension(group.IsChannel ? "Subscribers" : "Members", group.MemberCount);

            Description.Content = Strings.Resources.DescriptionPlaceholder;

            Verified.Visibility = group.IsVerified ? Visibility.Visible : Visibility.Collapsed;

            Username.Badge = $"{group.Username}";
            Username.Visibility = string.IsNullOrEmpty(group.Username) ? Visibility.Collapsed : Visibility.Visible;

            Location.Visibility = group.HasLocation ? Visibility.Visible : Visibility.Collapsed;

            if (group.IsChannel && group.Status is not ChatMemberStatusCreator && group.Status is not ChatMemberStatusLeft && group.Status is not ChatMemberStatusBanned)
            {
                MiscPanel.Visibility = Visibility.Visible;
                GroupLeave.Visibility = Visibility.Visible;
            }
            else
            {
                MiscPanel.Visibility = Visibility.Collapsed;
                GroupLeave.Visibility = Visibility.Collapsed;
            }

            ChannelMembersPanel.Visibility = group.IsChannel && (group.Status is ChatMemberStatusCreator || group.Status is ChatMemberStatusAdministrator) ? Visibility.Visible : Visibility.Collapsed;
            MembersPanel.Visibility = group.IsChannel ? Visibility.Collapsed : Visibility.Collapsed;
            //Admins.Visibility = Visibility.Collapsed;
            //Banned.Visibility = Visibility.Collapsed;
            //Restricted.Visibility = Visibility.Collapsed;
            //Members.Visibility = Visibility.Collapsed;

            Call.Visibility = Visibility.Collapsed;
            VideoCall.Visibility = Visibility.Collapsed;

            Edit.Visibility = group.Status is ChatMemberStatusCreator or ChatMemberStatusAdministrator ? Visibility.Visible : Visibility.Collapsed;
            Edit.Glyph = Icons.Edit;
            Edit.Content = group.IsChannel ? Strings.Resources.ManageChannelMenu : Strings.Resources.ManageGroupMenu;

            GroupInvite.Visibility = !group.IsChannel && (group.Status is ChatMemberStatusCreator || (group.Status is ChatMemberStatusAdministrator administrator && administrator.CanInviteUsers) || chat.Permissions.CanInviteUsers) ? Visibility.Visible : Visibility.Collapsed;

            // Unused:
            UserPhone.Visibility = Visibility.Collapsed;
            //UserCommonChats.Visibility = Visibility.Collapsed;
            UserStartSecret.Visibility = Visibility.Collapsed;
            SecretLifetime.Visibility = Visibility.Collapsed;
            SecretHashKey.Visibility = Visibility.Collapsed;
        }

        public void UpdateSupergroupFullInfo(Chat chat, Supergroup group, SupergroupFullInfo fullInfo)
        {
            GetEntities(fullInfo.Description);
            Description.Visibility = string.IsNullOrEmpty(fullInfo.Description) ? Visibility.Collapsed : Visibility.Visible;

            Location.Visibility = fullInfo.Location != null ? Visibility.Visible : Visibility.Collapsed;
            Location.Badge = fullInfo.Location?.Address;

            Admins.Badge = fullInfo.AdministratorCount;
            //Admins.Visibility = fullInfo.AdministratorCount > 0 ? Visibility.Visible : Visibility.Collapsed;

            Banned.Badge = fullInfo.BannedCount;
            //Banned.Visibility = fullInfo.BannedCount > 0 ? Visibility.Visible : Visibility.Collapsed;

            //Restricted.Badge = fullInfo.RestrictedCount;
            //Restricted.Visibility = fullInfo.RestrictedCount > 0 ? Visibility.Visible : Visibility.Collapsed;

            Members.Badge = fullInfo.MemberCount;
            //Members.Visibility = fullInfo.CanGetMembers && group.IsChannel ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Context menu

        private void About_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            MessageHelper.Hyperlink_ContextRequested(ViewModel.TranslateService, sender, args);
        }

        private void About_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }

        private void Description_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var flyout = FlyoutBase.GetAttachedFlyout(sender as FrameworkElement) as MenuFlyout;
            if (flyout == null)
            {
                return;
            }

            if (args.TryGetPosition(sender, out Point point))
            {
                if (point.X < 0 || point.Y < 0)
                {
                    point = new Point(Math.Max(point.X, 0), Math.Max(point.Y, 0));
                }

                flyout.ShowAt(sender, point);
            }
            else
            {
                flyout.ShowAt(sender as FrameworkElement);
            }
        }

        #endregion


        #region Context menu

        private void Menu_ContextRequested(object sender, RoutedEventArgs e)
        {
            var flyout = new MenuFlyout();

            var chat = ViewModel.Chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypePrivate or ChatTypeSecret)
            {
                var userId = chat.Type is ChatTypePrivate privata ? privata.UserId : chat.Type is ChatTypeSecret secret ? secret.UserId : 0;
                if (userId != ViewModel.CacheService.Options.MyId)
                {
                    var user = ViewModel.CacheService.GetUser(userId);
                    if (user == null)
                    {
                        return;
                    }

                    var fullInfo = ViewModel.CacheService.GetUserFull(userId);
                    if (fullInfo == null)
                    {
                        return;
                    }

                    //if (fullInfo.CanBeCalled)
                    //{
                    //    callItem = menu.addItem(call_item, R.drawable.ic_call_white_24dp);
                    //}
                    if (user.IsContact)
                    {
                        flyout.CreateFlyoutItem(ViewModel.ShareCommand, Strings.Resources.ShareContact, new FontIcon { Glyph = Icons.Share });
                        flyout.CreateFlyoutItem(chat.IsBlocked ? ViewModel.UnblockCommand : ViewModel.BlockCommand, chat.IsBlocked ? Strings.Resources.Unblock : Strings.Resources.BlockContact, new FontIcon { Glyph = chat.IsBlocked ? Icons.Block : Icons.Block });
                        flyout.CreateFlyoutItem(ViewModel.EditCommand, Strings.Resources.EditContact, new FontIcon { Glyph = Icons.Edit });
                        flyout.CreateFlyoutItem(ViewModel.DeleteCommand, Strings.Resources.DeleteContact, new FontIcon { Glyph = Icons.Delete });
                    }
                    else
                    {
                        if (user.Type is UserTypeBot bot)
                        {
                            if (bot.CanJoinGroups)
                            {
                                flyout.CreateFlyoutItem(ViewModel.InviteCommand, Strings.Resources.BotInvite, new FontIcon { Glyph = Icons.PersonAdd });
                            }

                            flyout.CreateFlyoutItem(() => { }, Strings.Resources.BotShare, new FontIcon { Glyph = Icons.Share });
                        }
                        else
                        {
                            flyout.CreateFlyoutItem(ViewModel.AddCommand, Strings.Resources.AddContact, new FontIcon { Glyph = Icons.PersonAdd });
                        }

                        if (user.PhoneNumber.Length > 0)
                        {
                            flyout.CreateFlyoutItem(ViewModel.ShareCommand, Strings.Resources.ShareContact, new FontIcon { Glyph = Icons.Share });
                            flyout.CreateFlyoutItem(chat.IsBlocked ? ViewModel.UnblockCommand : ViewModel.BlockCommand, chat.IsBlocked ? Strings.Resources.Unblock : Strings.Resources.BlockContact, new FontIcon { Glyph = chat.IsBlocked ? Icons.Block : Icons.Block });
                        }
                        else
                        {
                            if (user.Type is UserTypeBot)
                            {
                                flyout.CreateFlyoutItem(chat.IsBlocked ? ViewModel.UnblockCommand : ViewModel.BlockCommand, chat.IsBlocked ? Strings.Resources.BotRestart : Strings.Resources.BotStop, new FontIcon { Glyph = chat.IsBlocked ? Icons.Block : Icons.Block });
                            }
                            else
                            {
                                flyout.CreateFlyoutItem(chat.IsBlocked ? ViewModel.UnblockCommand : ViewModel.BlockCommand, chat.IsBlocked ? Strings.Resources.Unblock : Strings.Resources.BlockContact, new FontIcon { Glyph = chat.IsBlocked ? Icons.Block : Icons.Block });
                            }
                        }
                    }
                }
                else
                {
                    flyout.CreateFlyoutItem(ViewModel.ShareCommand, Strings.Resources.ShareContact, new FontIcon { Glyph = Icons.Share });
                }
            }
            //if (writeButton != null)
            //{
            //    boolean isChannel = ChatObject.isChannel(currentChat);
            //    if (isChannel && !ChatObject.canChangeChatInfo(currentChat) || !isChannel && !currentChat.admin && !currentChat.creator && currentChat.admins_enabled)
            //    {
            //        writeButton.setImageResource(R.drawable.floating_message);
            //        writeButton.setPadding(0, AndroidUtilities.dp(3), 0, 0);
            //    }
            //    else
            //    {
            //        writeButton.setImageResource(R.drawable.floating_camera);
            //        writeButton.setPadding(0, 0, 0, 0);
            //    }
            //}
            if (chat.Type is ChatTypeSupergroup super)
            {
                var supergroup = ViewModel.ProtoService.GetSupergroup(super.SupergroupId);
                if (supergroup == null)
                {
                    return;
                }

                var fullInfo = ViewModel.ProtoService.GetSupergroupFull(super.SupergroupId);

                if (supergroup.Status is ChatMemberStatusCreator or ChatMemberStatusAdministrator)
                {
                    if (chat.VideoChat.GroupCallId == 0 && supergroup.CanManageVideoChats())
                    {
                        flyout.CreateFlyoutItem(ViewModel.CallCommand, false, supergroup.IsChannel ? Strings.Resources.StartVoipChannel : Strings.Resources.StartVoipChat, new FontIcon { Glyph = Icons.VideoChat });
                    }

                    if (supergroup.IsChannel)
                    {
                        //flyout.CreateFlyoutItem(ViewModel.EditCommand, Strings.Resources.ManageChannelMenu, new FontIcon { Glyph = Icons.Edit });
                    }
                    else
                    {
                        flyout.CreateFlyoutItem(ViewModel.EditCommand, Strings.Resources.ManageGroupMenu, new FontIcon { Glyph = Icons.Edit });
                    }
                }

                if (fullInfo != null && fullInfo.CanGetStatistics)
                {
                    flyout.CreateFlyoutItem(ViewModel.StatisticsCommand, Strings.Resources.Statistics, new FontIcon { Glyph = Icons.DataUsage });
                }

                if (!super.IsChannel)
                {
                    flyout.CreateFlyoutItem(ViewModel.MembersCommand, Strings.Resources.SearchMembers, new FontIcon { Glyph = Icons.Search });

                    if (supergroup.Status is not ChatMemberStatusCreator and not ChatMemberStatusLeft and not ChatMemberStatusBanned)
                    {
                        flyout.CreateFlyoutItem(ViewModel.DeleteCommand, Strings.Resources.LeaveMegaMenu, new FontIcon { Glyph = Icons.Delete });
                    }
                }
                else if (supergroup.HasLinkedChat)
                {
                    flyout.CreateFlyoutItem(ViewModel.DiscussCommand, Strings.Resources.ViewDiscussion, new FontIcon { Glyph = Icons.Comment });
                }
            }
            else if (chat.Type is ChatTypeBasicGroup basic)
            {
                var basicGroup = ViewModel.ProtoService.GetBasicGroup(basic.BasicGroupId);
                if (basicGroup == null)
                {
                    return;
                }

                if (chat.VideoChat.GroupCallId == 0 && basicGroup.CanManageVideoChats())
                {
                    flyout.CreateFlyoutItem(ViewModel.CallCommand, false, Strings.Resources.StartVoipChat, new FontIcon { Glyph = Icons.VideoChat });
                }

                if (chat.Permissions.CanChangeInfo || basicGroup.Status is ChatMemberStatusCreator || basicGroup.Status is ChatMemberStatusAdministrator)
                {
                    flyout.CreateFlyoutItem(ViewModel.EditCommand, Strings.Resources.ChannelEdit, new FontIcon { Glyph = Icons.Edit });
                }

                flyout.CreateFlyoutItem(ViewModel.MembersCommand, Strings.Resources.SearchMembers, new FontIcon { Glyph = Icons.Search });

                flyout.CreateFlyoutItem(ViewModel.DeleteCommand, Strings.Resources.DeleteAndExit, new FontIcon { Glyph = Icons.Delete });
            }

            //flyout.CreateFlyoutItem(null, Strings.Resources.AddShortcut, new FontIcon { Glyph = Icons.Pin });

            if (flyout.Items.Count > 0)
            {
                flyout.ShowAt(sender as Button, new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
            }
        }

        #endregion

        #region Entities

        private void GetEntities(string text)
        {
            DescriptionSpan.Inlines.Clear();
            Description.BadgeLabel = text;

            var response = ViewModel.ProtoService.Execute(new GetTextEntities(text));
            if (response is TextEntities entities)
            {
                ReplaceEntities(DescriptionSpan, text, entities.Entities);
            }
            else
            {
                DescriptionSpan.Inlines.Add(new Run { Text = text });
            }
        }

        private void ReplaceEntities(Span span, string text, IList<TextEntity> entities)
        {
            var previous = 0;

            foreach (var entity in entities.OrderBy(x => x.Offset))
            {
                if (entity.Offset > previous)
                {
                    span.Inlines.Add(new Run { Text = text.Substring(previous, entity.Offset - previous) });
                }

                if (entity.Length + entity.Offset > text.Length)
                {
                    previous = entity.Offset + entity.Length;
                    continue;
                }

                if (entity.Type is TextEntityTypeBold)
                {
                    span.Inlines.Add(new Run { Text = text.Substring(entity.Offset, entity.Length), FontWeight = FontWeights.SemiBold });
                }
                else if (entity.Type is TextEntityTypeItalic)
                {
                    span.Inlines.Add(new Run { Text = text.Substring(entity.Offset, entity.Length), FontStyle = FontStyle.Italic });
                }
                else if (entity.Type is TextEntityTypeCode)
                {
                    span.Inlines.Add(new Run { Text = text.Substring(entity.Offset, entity.Length), FontFamily = new FontFamily("Consolas") });
                }
                else if (entity.Type is TextEntityTypePre or TextEntityTypePreCode)
                {
                    // TODO any additional
                    span.Inlines.Add(new Run { Text = text.Substring(entity.Offset, entity.Length), FontFamily = new FontFamily("Consolas") });
                }
                else if (entity.Type is TextEntityTypeUrl or TextEntityTypeEmailAddress or TextEntityTypePhoneNumber or TextEntityTypeMention or TextEntityTypeHashtag or TextEntityTypeCashtag or TextEntityTypeBotCommand)
                {
                    var hyperlink = new Hyperlink();
                    var data = text.Substring(entity.Offset, entity.Length);

                    hyperlink.Click += (s, args) => Entity_Click(entity.Type, data);
                    hyperlink.Inlines.Add(new Run { Text = data });
                    //hyperlink.Foreground = foreground;
                    span.Inlines.Add(hyperlink);

                    if (entity.Type is TextEntityTypeUrl)
                    {
                        MessageHelper.SetEntityData(hyperlink, data);
                    }
                }
                else if (entity.Type is TextEntityTypeTextUrl or TextEntityTypeMentionName)
                {
                    var hyperlink = new Hyperlink();
                    object data;
                    if (entity.Type is TextEntityTypeTextUrl textUrl)
                    {
                        data = textUrl.Url;
                        MessageHelper.SetEntityData(hyperlink, textUrl.Url);
                        ToolTipService.SetToolTip(hyperlink, textUrl.Url);
                    }
                    else if (entity.Type is TextEntityTypeMentionName mentionName)
                    {
                        data = mentionName.UserId;
                    }

                    hyperlink.Click += (s, args) => Entity_Click(entity.Type, null);
                    hyperlink.Inlines.Add(new Run { Text = text.Substring(entity.Offset, entity.Length) });
                    //hyperlink.Foreground = foreground;
                    span.Inlines.Add(hyperlink);
                }

                previous = entity.Offset + entity.Length;
            }

            if (text.Length > previous)
            {
                span.Inlines.Add(new Run { Text = text.Substring(previous) });
            }
        }

        private void Entity_Click(TextEntityType type, string data)
        {
            if (type is TextEntityTypeBotCommand)
            {

            }
            else if (type is TextEntityTypeEmailAddress)
            {
                ViewModel.OpenUrl("mailto:" + data, false);
            }
            else if (type is TextEntityTypePhoneNumber)
            {
                ViewModel.OpenUrl("tel:" + data, false);
            }
            else if (type is TextEntityTypeHashtag or TextEntityTypeCashtag)
            {

            }
            else if (type is TextEntityTypeMention)
            {
                ViewModel.OpenUsername(data);
            }
            else if (type is TextEntityTypeMentionName mentionName)
            {
                ViewModel.OpenUser(mentionName.UserId);
            }
            else if (type is TextEntityTypeTextUrl textUrl)
            {
                ViewModel.OpenUrl(textUrl.Url, true);
            }
            else if (type is TextEntityTypeUrl)
            {
                ViewModel.OpenUrl(data, false);
            }
        }

        #endregion
    }
}
