﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.View.Animation;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using AT.Markushi.UI;
using Bumptech.Glide.Integration.RecyclerView;
using Bumptech.Glide.Util;
using Com.Theartofdev.Edmodo.Cropper;
using Hani.Momanii.Supernova_emoji_library.Actions;
using Hani.Momanii.Supernova_emoji_library.Helper;
using Java.IO;
using Newtonsoft.Json;
using QuickDate.Activities.Chat.Adapters;
using QuickDate.Activities.Chat.Fragments;
using QuickDate.Activities.Tabbes;
using QuickDate.Helpers.Ads;
using QuickDate.Helpers.Controller;
using QuickDate.Helpers.Model;
using QuickDate.Helpers.Utils;
using QuickDate.SQLite;
using QuickDateClient.Classes.Chat;
using QuickDateClient.Classes.Global;
using QuickDateClient.Requests;
using ActionMode = Android.Support.V7.View.ActionMode;
using Console = System.Console;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using Object = Java.Lang.Object;
using SupportFragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Uri = Android.Net.Uri;

namespace QuickDate.Activities.Chat
{
    [Activity(Icon = "@drawable/icon", Theme = "@style/MyTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleInstance)]
    public class MessagesBoxActivity : AppCompatActivity, IOnClickListenerSelectedMessages
    {
        #region Variables Basic

        public AppCompatImageView ChatEmojisImage;
        public RelativeLayout RootView;
        public EmojiconEditText EmojisIconEditTextView;
        public CircleButton ChatSendButton, StickerButton, GiftButton, ImageButton;
        public static Toolbar TopChatToolBar;
        public RecyclerView ChatBoxRecyclerView;
        public LinearLayoutManager MLayoutManager;
        public static UserMessagesAdapter MAdapter;
        public string LastSeenUser = "", TaskWork = "";
        public static int UnixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        public string Time = Convert.ToString(UnixTimestamp);
        public int BeforeMessageId, FirstMessageId;
        public static int Userid;// to_id
        public static Timer Timer;
        public SwipeRefreshLayout SwipeRefreshLayout;
        public UserInfoObject UserInfoData;
        public ActionModeCallback ModeCallback;
        public static ActionMode ActionMode;
        public FrameLayout TopFragmentHolder ,ButtonFragmentHolder;
        public StickersFragment ChatStickersFragment;
        public GiftFragment ChatGiftFragment;
        public SupportFragment MainFragmentOpened;
        public FastOutSlowInInterpolator Interpolation;
        public HomeActivity GlobalContext;

        public static MessagesBoxActivity Instance;

        #endregion

        #region General

        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                Window.SetSoftInputMode(SoftInput.AdjustResize);
                base.OnCreate(savedInstanceState);
                Window.SetBackgroundDrawableResource(Resource.Drawable.chatBackground);

                IMethods.IApp.FullScreenApp(this);

                // Set our view from the "MessagesBox_Layout" layout resource
                SetContentView(Resource.Layout.MessagesBoxLayout);

                Instance = this;

                var data = Intent.GetStringExtra("UserId") ?? "Data not available";
                if (data != "Data not available" && !string.IsNullOrEmpty(data)) Userid = int.Parse(data); // to_id

                string json = Intent.GetStringExtra("UserItem");
                var item = JsonConvert.DeserializeObject<UserInfoObject>(json);
                if (item != null) UserInfoData = item;

                Interpolation = new FastOutSlowInInterpolator();

                ChatStickersFragment = new StickersFragment();
                ChatGiftFragment = new GiftFragment();

                Bundle args = new Bundle();
                args.PutString("userid", Userid.ToString());
                ChatStickersFragment.Arguments = args;
                ChatGiftFragment.Arguments = args;
                 
                //Get Value And Set Toolbar
                InitComponent();
                InitToolbar();
                SetRecyclerViewAdapters();
                GetStickersGiftsLists();

                //Set Title ToolBar and data chat user After that get messages 
                loadData_ItemUser();

                AdsGoogle.Ad_RewardedVideo(this);

                GlobalContext = HomeActivity.GetInstance();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static MessagesBoxActivity GetInstance()
        {
            try
            {
                return Instance;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        protected override void OnResume()
        {
            try
            {
                base.OnResume();
                AddOrRemoveEvent(true);

                if (Timer != null)
                {
                    Timer.Enabled = true;
                    Timer.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected override void OnPause()
        {
            try
            {
                base.OnPause();
                AddOrRemoveEvent(false);

                if (Timer != null)
                {
                    Timer.Enabled = false;
                    Timer.Stop(); 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void OnTrimMemory(TrimMemory level)
        {
            try
            {
                
                
                
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                base.OnTrimMemory(level);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public override void OnLowMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration);
                base.OnLowMemory();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                if (Timer != null)
                {
                    Timer.Enabled = false;
                    Timer.Stop();
                    Timer = null;
                }

                MAdapter?.MessageList.Clear();
                MAdapter?.NotifyDataSetChanged();

                base.OnDestroy();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion

        #region Functions

        private void InitComponent()
        {
            try
            {
                RootView = FindViewById<RelativeLayout>(Resource.Id.rootChatWindowView);

                ChatEmojisImage = FindViewById<AppCompatImageView>(Resource.Id.emojiicon);
                EmojisIconEditTextView = FindViewById<EmojiconEditText>(Resource.Id.EmojiconEditText5);
                ChatSendButton = FindViewById<CircleButton>(Resource.Id.sendButton);
                ChatBoxRecyclerView = FindViewById<RecyclerView>(Resource.Id.recyler);
                SwipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout);
                SwipeRefreshLayout.SetColorSchemeResources(Android.Resource.Color.HoloBlueLight, Android.Resource.Color.HoloGreenLight, Android.Resource.Color.HoloOrangeLight, Android.Resource.Color.HoloRedLight);
                StickerButton = FindViewById<CircleButton>(Resource.Id.stickerButton);
                GiftButton = FindViewById<CircleButton>(Resource.Id.giftButton);
                ImageButton = FindViewById<CircleButton>(Resource.Id.imageButton);
                TopFragmentHolder = FindViewById<FrameLayout>(Resource.Id.TopFragmentHolder);
                ButtonFragmentHolder = FindViewById<FrameLayout>(Resource.Id.ButtomFragmentHolder);
                SupportFragmentManager.BeginTransaction().Add(ButtonFragmentHolder.Id, ChatStickersFragment, "ChatStickersFragment");

                StickerButton.Tag = "Closed";
                GiftButton.Tag = "Closed";

                ChatSendButton.Tag = "Text";
                ChatSendButton.SetImageResource(Resource.Drawable.SendLetter);

                ModeCallback = new ActionModeCallback(this);

                var emojisIcon = new EmojIconActions(this, RootView, EmojisIconEditTextView, ChatEmojisImage);
                emojisIcon.ShowEmojIcon(); 
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void InitToolbar()
        {
            try
            {
                TopChatToolBar = FindViewById<Toolbar>(Resource.Id.toolbar);
                if (TopChatToolBar != null)
                {
                    TopChatToolBar.SetTitleTextColor(Color.Black);
                    SetSupportActionBar(TopChatToolBar);
                    SupportActionBar.SetDisplayShowCustomEnabled(true);
                    SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    SupportActionBar.SetHomeButtonEnabled(true);
                    SupportActionBar.SetDisplayShowHomeEnabled(true); 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void SetRecyclerViewAdapters()
        {
            try
            {
               
                MAdapter = new UserMessagesAdapter(this);

                ChatBoxRecyclerView.SetItemAnimator(null);

                MLayoutManager = new LinearLayoutManager(this);
                ChatBoxRecyclerView.SetLayoutManager(MLayoutManager);
                ChatBoxRecyclerView.SetItemViewCacheSize(20);
                ChatBoxRecyclerView.HasFixedSize = true;
                ChatBoxRecyclerView.GetLayoutManager().ItemPrefetchEnabled = true;
                var sizeProvider = new FixedPreloadSizeProvider(10, 10);
                var preLoader = new RecyclerViewPreloader<GetChatConversationsObject.Messages>(this, MAdapter, sizeProvider, 10);
                ChatBoxRecyclerView.AddOnScrollListener(preLoader);
                ChatBoxRecyclerView.SetAdapter(MAdapter);

                MAdapter.SetOnClickListener(this);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void AddOrRemoveEvent(bool addEvent)
        {
            try
            {
                // true +=  // false -=
                if (addEvent)
                {
                    ChatSendButton.Touch += Chat_sendButton_Touch;
                    StickerButton.Click += StickerButtonOnClick;
                    GiftButton.Click += GiftButtonOnClick;
                    ImageButton.Click += ImageButtonOnClick;
                }
                else
                {
                    ChatSendButton.Touch -= Chat_sendButton_Touch;
                    StickerButton.Click -= StickerButtonOnClick;
                    GiftButton.Click -= GiftButtonOnClick;
                    ImageButton.Click -= ImageButtonOnClick;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
         
        #endregion

        #region Set ToolBar and data chat user

        //Set ToolBar and data chat user
        public void loadData_ItemUser()
        {
            try
            {
                if (UserInfoData != null)
                {
                    SupportActionBar.Title = QuickDateTools.GetNameFinal(UserInfoData);  
                    SupportActionBar.Subtitle = GetString(Resource.String.Lbl_Last_seen) + " " + IMethods.Time.TimeAgo(UserInfoData.Lastseen);
                    LastSeenUser = GetString(Resource.String.Lbl_Last_seen) + " " + IMethods.Time.TimeAgo(UserInfoData.Lastseen);
                }
                 
                Get_Messages();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
         
        #endregion

        #region Get Messages

        //Get Messages Local Or Api
        public void Get_Messages()
        {
            try
            {
                BeforeMessageId = 0;
                MAdapter.MessageList.Clear();
                MAdapter.NotifyDataSetChanged();

                SqLiteDatabase dbDatabase = new SqLiteDatabase();
                var localList = dbDatabase.GetMessagesList(UserDetails.UserId, Userid, BeforeMessageId);
                if (localList == "1") //Database.. Get Messages Local
                {
                    MAdapter.NotifyDataSetChanged();

                    //Scroll Down >> 
                    ChatBoxRecyclerView.ScrollToPosition(MAdapter.MessageList.Count - 1);
                    SwipeRefreshLayout.Refreshing = false;
                    SwipeRefreshLayout.Enabled = false;
                }
                else //Or server.. Get Messages Api
                {
                    SwipeRefreshLayout.Refreshing = true;
                    SwipeRefreshLayout.Enabled = true;
                    GetMessages_API();
                }

                //Set Event Scroll
                XamarinRecyclerViewOnScrollListener onScrollListener = new XamarinRecyclerViewOnScrollListener(MLayoutManager, SwipeRefreshLayout);
                onScrollListener.LoadMoreEvent += Messages_OnScroll_OnLoadMoreEvent;
                ChatBoxRecyclerView.AddOnScrollListener(onScrollListener);
                TaskWork = "Working";

                //Run timer
                Timer = new Timer { Interval = AppSettings.MessageRequestSpeed, Enabled = true };
                Timer.Elapsed += TimerOnElapsed_MessageUpdater;
                Timer.Start();

                dbDatabase.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //Get Messages From API 
        public async void GetMessages_API()
        {
            try
            {
                if (!IMethods.CheckConnectivity())
                {
                    SwipeRefreshLayout.Refreshing = false;
                    SwipeRefreshLayout.Enabled = false;

                    Toast.MakeText(this,GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short);
                }
                else
                {
                    BeforeMessageId = 0;

                    var (apiStatus, respond) = await RequestsAsync.Chat.GetChatConversationsAsync(Userid.ToString());
                    if (apiStatus == 200)
                    {
                        if (respond is GetChatConversationsObject result)
                        {
                            if (result.data.Count > 0)
                            {
                                MAdapter.MessageList = new ObservableCollection<GetChatConversationsObject.Messages>(result.data);
                                MAdapter.NotifyDataSetChanged();

                                //Insert to DataBase
                                SqLiteDatabase dbDatabase = new SqLiteDatabase();
                                dbDatabase.InsertOrReplaceMessages(MAdapter.MessageList);
                                dbDatabase.Dispose();

                                //Scroll Down >> 
                                ChatBoxRecyclerView.ScrollToPosition(MAdapter.MessageList.Count - 1);

                                SwipeRefreshLayout.Refreshing = false;
                                SwipeRefreshLayout.Enabled = false;
                            }
                        }
                    }
                    else if (apiStatus == 400)
                    {
                        if (respond is ErrorObject error)
                        {
                            var errorText = error.ErrorData.ErrorText;

                            if (errorText.Contains("Permission Denied"))
                                ApiRequest.Logout(this);
                        }
                    }
                    else if (apiStatus == 404)
                    {
                        var error = respond.ToString();
                        //Toast.MakeText(this, error, ToastLength.Short).Show();
                    }

                    SwipeRefreshLayout.Refreshing = false;
                    SwipeRefreshLayout.Enabled = false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                SwipeRefreshLayout.Refreshing = false;
                SwipeRefreshLayout.Enabled = false;
            }
        }

        #endregion

        //Timer Message Updater >> Get New Message
        private void TimerOnElapsed_MessageUpdater(object sender, ElapsedEventArgs e)
        {
            try
            {
                //Code get last Message id where Updater >>
                MessageUpdater();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #region Updater

        public async void MessageUpdater()
        {
            try
            {
                if (TaskWork == "Working")
                {
                    TaskWork = "Stop";

                    if (!IMethods.CheckConnectivity())
                    {
                        SwipeRefreshLayout.Refreshing = false;
                        Toast.MakeText(this,GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short);
                    }
                    else
                    {
                        int countList = MAdapter.MessageList.Count;
                        string afterId = MAdapter.MessageList.LastOrDefault()?.Id.ToString() ?? "";
                        var (apiStatus, respond) = await RequestsAsync.Chat.GetChatConversationsAsync(Userid.ToString(), "30", afterId);
                        if (apiStatus == 200)
                        {
                            if (respond is GetChatConversationsObject result)
                            {
                                int responseList = result.data.Count;
                                if (responseList > 0)
                                {
                                    if (countList > 0)
                                    {
                                        foreach (var item in result.data)
                                        {
                                            var check = MAdapter.MessageList.FirstOrDefault(a => a.Id == item.Id);
                                            if (check == null)
                                            {
                                                MAdapter.MessageList.Add(item);

                                                int index = MAdapter.MessageList.IndexOf(item);
                                                if (index > -1 )
                                                {
                                                    RunOnUiThread(() =>
                                                    {
                                                        try
                                                        {
                                                            MAdapter.NotifyItemInserted(index);

                                                            //Scroll Down >> 
                                                            ChatBoxRecyclerView.ScrollToPosition(MAdapter.MessageList.Count - 1);
                                                        }
                                                        catch (Exception ee)
                                                        { 
                                                            Console.WriteLine(ee);
                                                        } 
                                                    });
                                                } 
                                            }
                                        }
                                    }
                                    else
                                    {
                                        MAdapter.MessageList = new ObservableCollection<GetChatConversationsObject.Messages>(result.data);
                                    }

                                    RunOnUiThread(() =>
                                    {
                                        var lastCountItem = MAdapter.ItemCount;
                                        if (countList > 0)
                                            MAdapter.NotifyItemRangeInserted(lastCountItem, MAdapter.MessageList.Count - 1);
                                        else
                                            MAdapter.NotifyDataSetChanged();

                                        //Insert to DataBase
                                        SqLiteDatabase dbDatabase = new SqLiteDatabase();
                                        dbDatabase.InsertOrReplaceMessages(MAdapter.MessageList);
                                        dbDatabase.Dispose();

                                        //Scroll Down >> 
                                        ChatBoxRecyclerView.ScrollToPosition(MAdapter.MessageList.Count - 1);

                                        var lastMessage = MAdapter.MessageList.LastOrDefault();
                                        if (lastMessage != null)
                                        { 
                                            var dataUser = LastChatActivity.MAdapter.UserList?.FirstOrDefault(a => a.User.Id == Userid);
                                            if (dataUser != null)
                                            {
                                                dataUser.Text = lastMessage.Text;
                                                int index = LastChatActivity.MAdapter.UserList.IndexOf(dataUser);

                                                LastChatActivity.MAdapter.UserList.Move(index, 0);
                                                LastChatActivity.MAdapter.NotifyItemMoved(index, 0);

                                                var data = LastChatActivity.MAdapter.UserList.FirstOrDefault(a => a.User.Id == dataUser.User.Id);
                                                if (data != null)
                                                {
                                                    data.Id = dataUser.Id;
                                                    data.Owner = dataUser.Owner;
                                                    data.User = dataUser.User;
                                                    data.Seen = dataUser.Seen;
                                                    data.Text = dataUser.Text;
                                                    data.Media = dataUser.Media;
                                                    data.Sticker = dataUser.Sticker;
                                                    data.Time = dataUser.Time;
                                                    data.CreatedAt = dataUser.CreatedAt;
                                                    data.NewMessages = dataUser.NewMessages;
                                                    data.MessageType = dataUser.MessageType;

                                                    LastChatActivity.MAdapter.NotifyItemChanged(LastChatActivity.MAdapter.UserList.IndexOf(data));
                                                } 
                                            } 
                                        }

                                        if (AppSettings.RunSoundControl)
                                            IMethods.AudioRecorderAndPlayer.PlayAudioFromAsset("Popup_GetMesseges.mp3");

                                    });
                                }
                            }
                        }
                        else if (apiStatus == 400)
                        {
                            if (respond is ErrorObject error)
                            {
                                var errorText = error.ErrorData.ErrorText;
                                if (errorText.Contains("Permission Denied"))
                                    ApiRequest.Logout(this);
                            }
                        }
                        else if (apiStatus == 404)
                        {
                            var error = respond.ToString();
                            //Toast.MakeText(this, error, ToastLength.Short).Show();
                        }
                    }
                    TaskWork = "Working";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                TaskWork = "Working";
            }
        }

        public static void UpdateOneMessage(GetChatConversationsObject.Messages message)
        {
            try
            { 
                var checker = MAdapter.MessageList.FirstOrDefault(a => a.Id == message.Id);
                if (checker != null)
                {
                    checker.Id = message.Id;
                    checker.FromName = message.FromName;
                    checker.FromAvater = message.FromAvater;
                    checker.ToName = message.ToName;
                    checker.ToAvater = message.ToAvater;
                    checker.From = message.From;
                    checker.To = message.To;
                    checker.Text = message.Text;
                    checker.Media = message.Media;
                    checker.FromDelete = message.FromDelete;
                    checker.ToDelete = message.ToDelete;
                    checker.Sticker = message.Sticker;
                    checker.CreatedAt = message.CreatedAt;
                    checker.Seen = message.Seen;
                    checker.Type = message.Type;
                    checker.MessageType = message.MessageType;

                    MAdapter.NotifyItemChanged(MAdapter.MessageList.IndexOf(checker)); 
                } 
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region Load More

        public async void LoadMoreMessages()
        {
            try
            {
                //Run Load More Api 
                var local = LoadMoreMessagesDatabase();
                if (local == "1")
                {

                }
                else
                {
                    var api = await LoadMoreMessagesApi();
                    if (api == "1")
                    {

                    }
                    else
                    {
                        SwipeRefreshLayout.Refreshing = false;
                        SwipeRefreshLayout.Enabled = false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public string LoadMoreMessagesDatabase()
        {
            try
            {
                SqLiteDatabase dbDatabase = new SqLiteDatabase();
                var localList = dbDatabase.GetMessageList(Convert.ToInt32(UserDetails.UserId), Userid, FirstMessageId);
                if (localList?.Count > 0) //Database.. Get Messages Local
                {
                    localList = new List<DataTables.MessageTb>(localList.OrderByDescending(a => a.Id));

                    foreach (var messages in localList)
                    {
                        GetChatConversationsObject.Messages message = new GetChatConversationsObject.Messages
                        {
                            Id = messages.Id,
                            FromName = messages.FromName,
                            FromAvater = messages.FromAvater,
                            ToName = messages.ToName,
                            ToAvater = messages.ToAvater,
                            From = messages.FromId,
                            To = messages.ToId,
                            Text = messages.Text,
                            Media = messages.Media,
                            FromDelete = messages.FromDelete,
                            ToDelete = messages.ToDelete,
                            Sticker = messages.Sticker,
                            CreatedAt = messages.CreatedAt,
                            Seen = messages.Seen,
                            Type = messages.Type,
                            MessageType = messages.MessageType
                        };

                        MAdapter.MessageList.Insert(0, message);
                        MAdapter.NotifyItemInserted(MAdapter.MessageList.IndexOf(MAdapter.MessageList.FirstOrDefault()));

                        var index = MAdapter.MessageList.FirstOrDefault(a => a.Id == FirstMessageId);
                        if (index != null)
                        {
                            MAdapter.NotifyItemChanged(MAdapter.MessageList.IndexOf(index));
                            //Scroll Down >> 
                            ChatBoxRecyclerView.ScrollToPosition(MAdapter.MessageList.IndexOf(index));
                        } 
                    }

                    dbDatabase.Dispose();
                    return "1";
                }

                dbDatabase.Dispose();
                return "0";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "0";
            }
        }

        public async Task<string> LoadMoreMessagesApi()
        {
            try
            {
                if (!IMethods.CheckConnectivity())
                {
                    SwipeRefreshLayout.Refreshing = false;
                    Toast.MakeText(this,GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short);
                }
                else
                {
                    var (apiStatus, respond) = await RequestsAsync.Chat.GetChatConversationsAsync(Userid.ToString(), "30", FirstMessageId.ToString(),"1").ConfigureAwait(false);
                    if (apiStatus == 200)
                    {
                        if (respond is GetChatConversationsObject result)
                        {
                            if (result.data.Count > 0)
                            {
                                var listApi = new ObservableCollection<GetChatConversationsObject.Messages>();

                                foreach (var messages in result.data.OrderBy(a => a.CreatedAt))
                                {
                                    MAdapter.MessageList.Insert(0, messages);
                                    MAdapter.NotifyItemInserted(MAdapter.MessageList.IndexOf(MAdapter.MessageList.FirstOrDefault()));

                                    var index = MAdapter.MessageList.FirstOrDefault(a => a.Id == FirstMessageId);
                                    if (index != null)
                                    {
                                        MAdapter.NotifyItemChanged(MAdapter.MessageList.IndexOf(index));
                                        //Scroll Down >> 
                                        ChatBoxRecyclerView.ScrollToPosition(MAdapter.MessageList.IndexOf(index));
                                    }
                                     
                                    listApi.Insert(0, messages);

                                    SqLiteDatabase dbDatabase = new SqLiteDatabase();
                                    // Insert data user in database
                                    dbDatabase.InsertOrReplaceMessages(listApi);
                                    dbDatabase.Dispose();
                                }
                                return "1";
                            }
                            return "0";
                        }
                    }
                    else if (apiStatus == 400)
                    {
                        if (respond is ErrorObject error)
                        {
                            var errorText = error.ErrorData.ErrorText;
                            if (errorText.Contains("Permission Denied"))
                                ApiRequest.Logout(this);
                        }
                    }
                    else if (apiStatus == 404)
                    {
                        var error = respond.ToString();
                        //Toast.MakeText(this, error, ToastLength.Short).Show();
                    }
                }
                return "0";
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return "0";
            }
        }

        #endregion

        #region Events

        //Send Message type => "right_text"
        public void OnClick_OfSendButton()
        {
            try
            {
                UnixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                var time2 = UnixTimestamp.ToString();
                string timeNow = DateTime.Now.ToString("hh:mm");

                if (string.IsNullOrEmpty(EmojisIconEditTextView.Text))
                {

                }
                else
                {
                    //Here on This function will send Text Messages to the user 

                    //remove \n in a string
                    string replacement = Regex.Replace(EmojisIconEditTextView.Text, @"\t|\n|\r", "");

                    if (IMethods.CheckConnectivity())
                    {
                        GetChatConversationsObject.Messages message = new GetChatConversationsObject.Messages
                        {
                            Id = UnixTimestamp,
                            FromName = UserDetails.FullName,
                            FromAvater = UserDetails.Avatar,
                            ToName = UserInfoData?.Fullname ?? "",
                            ToAvater = UserInfoData?.Avater ?? "",
                            From = UserDetails.UserId,
                            To = Userid,
                            Text = replacement,
                            Media = "",
                            FromDelete = 0,
                            ToDelete = 0,
                            Sticker = "",
                            CreatedAt = timeNow,
                            Seen = 0,
                            Type = "Sent",
                            MessageType = "text" 
                        };

                        MAdapter.MessageList.Add(message);
                         
                        int index = MAdapter.MessageList.IndexOf(MAdapter.MessageList.Last());
                        if (index > -1)
                        {
                            MAdapter.NotifyItemInserted(index);
                            //Scroll Down >> 
                            ChatBoxRecyclerView.ScrollToPosition(index);
                        }
                         
                        MessageController.SendMessageTask(Userid, EmojisIconEditTextView.Text, "", "", time2, UserInfoData).ConfigureAwait(false);
                    }
                    else
                    {
                        Toast.MakeText(this,GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short);
                    }

                    EmojisIconEditTextView.Text = "";
                }

                ChatSendButton.Tag = "Text";
                ChatSendButton.SetImageResource(Resource.Drawable.SendLetter);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //Send messages >>  type text
        private void Chat_sendButton_Touch(object sender, View.TouchEventArgs e)
        {
            try
            {
                if (e.Event.Action == MotionEventActions.Down)
                {
                    OnClick_OfSendButton();
                }
                e.Handled = false;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        //Sent image >> type media (OnActivityResult)
        private void ImageButtonOnClick(object sender, EventArgs e)
        {
            try
            {
                OpenDialogGallery();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        //Sent Sticker >> type sticker (ChatStickersFragment)
        private void StickerButtonOnClick(object sender, EventArgs e)
        {
            try
            {
                if (StickerButton.Tag.ToString() == "Closed")
                {
                    ResetButtonTags();
                    StickerButton.Tag = "Opened";
                    StickerButton.Drawable.SetTint(Color.ParseColor(AppSettings.MainColor));
                    ReplaceButtonFragment(ChatStickersFragment);
                }
                else
                {
                    ResetButtonTags();
                    StickerButton.Drawable.SetTint(Color.ParseColor("#424242"));
                    //TopFragmentHolder.Animate().SetInterpolator(Interpolation).TranslationY(1200).SetDuration(300);
                    SupportFragmentManager.BeginTransaction().Remove(ChatStickersFragment).Commit();
                } 
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        //Sent Gift >> type sticker (ChatGiftFragment)
        private void GiftButtonOnClick(object sender, EventArgs e)
        {
            try
            {
                if (GiftButton.Tag.ToString() == "Closed")
                {
                    ResetButtonTags();
                    GiftButton.Tag = "Opened";
                    GiftButton.Drawable.SetTint(Color.ParseColor(AppSettings.MainColor));
                    ReplaceButtonFragment(ChatGiftFragment);
                }
                else
                {
                    ResetButtonTags();
                    GiftButton.Drawable.SetTint(Color.ParseColor("#424242"));
                    //TopFragmentHolder.Animate().SetInterpolator(Interpolation).TranslationY(1200).SetDuration(300);
                    SupportFragmentManager.BeginTransaction().Remove(ChatGiftFragment).Commit();
                } 
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion

        #region Scroll

        //Event Scroll #Messages
        private void Messages_OnScroll_OnLoadMoreEvent(object sender, EventArgs eventArgs)
        {
            try
            {
                //Start Loader Get from Database or API Request >>
                SwipeRefreshLayout.Refreshing = true;
                SwipeRefreshLayout.Enabled = true;

                FirstMessageId = 0;

                //Code get first Message id where LoadMore >>
                var mes = MAdapter.MessageList.FirstOrDefault();
                if (mes != null)
                {
                    FirstMessageId = mes.Id;
                }

                if (FirstMessageId > 0)
                {
                    LoadMoreMessages();
                }

                SwipeRefreshLayout.Refreshing = false;
                SwipeRefreshLayout.Enabled = false;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public class XamarinRecyclerViewOnScrollListener : RecyclerView.OnScrollListener
        {
            public delegate void LoadMoreEventHandler(object sender, EventArgs e);

            public event LoadMoreEventHandler LoadMoreEvent;

            public LinearLayoutManager LayoutManager;
            public SwipeRefreshLayout SwipeRefreshLayout;

            public XamarinRecyclerViewOnScrollListener(LinearLayoutManager layoutManager, SwipeRefreshLayout swipeRefreshLayout)
            {
                LayoutManager = layoutManager;
                SwipeRefreshLayout = swipeRefreshLayout;
            }

            public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
            {
                try
                {
                    base.OnScrolled(recyclerView, dx, dy);

                    var visibleItemCount = recyclerView.ChildCount;
                    var totalItemCount = recyclerView.GetAdapter().ItemCount;

                    var pastVisibleItems = LayoutManager.FindFirstVisibleItemPosition();
                    if (pastVisibleItems == 0 && visibleItemCount != totalItemCount)
                    {
                        //Load More  from API Request
                        LoadMoreEvent?.Invoke(this, null);
                        //Start Load More messages From Database
                    }
                    else
                    {
                        if (SwipeRefreshLayout.Refreshing)
                        {
                            SwipeRefreshLayout.Refreshing = false;
                            SwipeRefreshLayout.Enabled = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        #endregion

        #region Menu

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MessagesBox_Menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                case Resource.Id.menu_block:
                    MenuBlockClick();
                    break;
                case Resource.Id.menu_clear_chat:
                    MenuClearChatClick();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        //Block User
        public async void MenuBlockClick()
        {
            try
            {
                if (IMethods.CheckConnectivity())
                {
                    var list = GlobalContext?.ProfileFragment?.FavoriteFragment?.FavoriteAdapter?.FavoritesList;
                    if (list?.Count > 0)
                    {
                        var dataFav = list.FirstOrDefault(a => a.Id == Userid);
                        if (dataFav != null)
                        {
                            list.Remove(dataFav);
                            GlobalContext?.ProfileFragment?.FavoriteFragment?.FavoriteAdapter.NotifyDataSetChanged();
                        }
                    }

                    // Remove in DB 
                    var sqlEntity = new SqLiteDatabase();
                    sqlEntity.Remove_Favorite(UserInfoData);
                    sqlEntity.Dispose();

                    MenuClearChatClick();
                     
                    Toast.MakeText(this, GetText(Resource.String.Lbl_Blocked_successfully), ToastLength.Short).Show();
                    var (apiStatus, respond) =await RequestsAsync.Users.BlockAsync(Userid.ToString()).ConfigureAwait(false);
                    Finish();
                }
                else
                {
                    Toast.MakeText(this, GetText(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short).Show();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async void MenuClearChatClick()
        {
            try
            {
                if (IMethods.CheckConnectivity())
                {
                    MAdapter.MessageList.Clear();
                    MAdapter.NotifyDataSetChanged();

                    var userDelete = LastChatActivity.MAdapter.UserList?.FirstOrDefault(a => a.User.Id == Userid);
                    if (userDelete != null)
                    {
                        LastChatActivity.MAdapter.UserList.Remove(userDelete);

                        var index = LastChatActivity.MAdapter.UserList.IndexOf(userDelete);
                        if (index > -1)
                            LastChatActivity.MAdapter.NotifyItemRemoved(index); 
                    }

                    SqLiteDatabase dbDatabase = new SqLiteDatabase();
                    dbDatabase.DeleteAllMessagesUser(UserDetails.UserId.ToString(), Userid.ToString());
                    dbDatabase.Dispose();

                    var (apiStatus, respond) = await RequestsAsync.Chat.DeleteMessagesAsync(Userid.ToString()).ConfigureAwait(false);
                }
                else
                {
                    Toast.MakeText(this, GetText(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short).Show();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region Toolbar & Selected

        public class ActionModeCallback : Object, ActionMode.ICallback
        {
            public MessagesBoxActivity Activity;
            public ActionModeCallback(MessagesBoxActivity activity)
            {
                Activity = activity;
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                int id = item.ItemId;
                if (id == Resource.Id.action_copy)
                {
                    CopyItems();
                    mode.Finish();
                    return true;
                }
                return false;
            }

            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                SetSystemBarColor(Activity, AppSettings.MainColor);
                mode.MenuInflater.Inflate(Resource.Menu.menuChat, menu);
                return true;
            }

            public void SetSystemBarColor(Activity act, string color)
            {
                try
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                    {
                        Window window = act.Window;
                        window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                        window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                        window.SetStatusBarColor(Color.ParseColor(color));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            public void OnDestroyActionMode(ActionMode mode)
            {
                try
                {
                    MAdapter.ClearSelections();
                    ActionMode.Finish();
                    ActionMode = null;

                    SetSystemBarColor(Activity, AppSettings.MainColor);

                    if (Timer != null)
                    {
                        Timer.Enabled = true;
                        Timer.Start();
                    }

                    TopChatToolBar.Visibility = ViewStates.Visible;
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                return false;
            }
             
            //Copy Messages
            private void CopyItems()
            {
                try
                {
                    if (Timer != null)
                    {
                        Timer.Enabled = true;
                        Timer.Start();
                    }

                    if (TopChatToolBar.Visibility != ViewStates.Visible)
                        TopChatToolBar.Visibility = ViewStates.Visible;

                    string allText = "";
                    List<int> selectedItemPositions = MAdapter.GetSelectedItems();
                    for (int i = selectedItemPositions.Count - 1; i >= 0; i--)
                    {
                        var datItem = MAdapter.GetItem(selectedItemPositions[i]);
                        if (datItem != null)
                        {
                            allText = allText + " \n" + datItem.Text;
                        }
                    }

                    ClipboardManager clipboard = (ClipboardManager)Activity.GetSystemService(ClipboardService);
                    ClipData clip = ClipData.NewPlainText("clipboard", allText);
                    clipboard.PrimaryClip = clip;

                    MAdapter.NotifyDataSetChanged();

                    Toast.MakeText(Activity, Activity.GetText(Resource.String.Lbl_Text_copied), ToastLength.Short).Show();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public void ItemClick(View view, GetChatConversationsObject.Messages obj, int pos)
        {
            try
            {
                if (MAdapter.GetSelectedItemCount() > 0) // Add Select New Item 
                {
                    EnableActionMode(pos);
                }
                else
                {
                    if (Timer != null)
                    {
                        Timer.Enabled = true;
                        Timer.Start();
                    }

                    if (TopChatToolBar.Visibility != ViewStates.Visible)
                        TopChatToolBar.Visibility = ViewStates.Visible;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void ItemLongClick(View view, GetChatConversationsObject.Messages obj, int pos)
        {
            EnableActionMode(pos);
        }

        private void EnableActionMode(int position)
        {
            try
            {  
                if (ActionMode == null)
                {
                    ActionMode = StartSupportActionMode(ModeCallback);
                }
                ToggleSelection(position);
            }
            catch (Exception e)
            {
                Console.WriteLine(e); 
            }
          
        }

        private void ToggleSelection(int position)
        {
            try
            {
                MAdapter.ToggleSelection(position);
                int count = MAdapter.GetSelectedItemCount();

                if (count == 0)
                {
                    if (Timer != null)
                    {
                        Timer.Enabled = true;
                        Timer.Start();
                    }

                    TopChatToolBar.Visibility = ViewStates.Visible;
                    ActionMode.Finish();
                }
                else
                {
                    if (Timer != null)
                    {
                        Timer.Enabled = false;
                        Timer.Stop();
                    }

                    TopChatToolBar.Visibility = ViewStates.Gone;
                    ActionMode.SetTitle(count);
                    ActionMode.Invalidate();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region SupportFragment

        public void ReplaceButtonFragment(SupportFragment fragmentView)
        {
            try
            {
                if (fragmentView != MainFragmentOpened)
                {
                    if (MainFragmentOpened == ChatStickersFragment)
                    {
                        StickerButton.Drawable.SetTint(Color.ParseColor("#424242"));
                    }
                }

                if (fragmentView.IsVisible)
                    return;

                var trans = SupportFragmentManager.BeginTransaction();
                trans.Replace(ButtonFragmentHolder.Id, fragmentView);

                if (SupportFragmentManager.BackStackEntryCount == 0)
                {
                    trans.AddToBackStack(null);
                }

                trans.Commit();

                ButtonFragmentHolder.TranslationY = 1200;
                ButtonFragmentHolder.Animate().SetInterpolator(new FastOutSlowInInterpolator()).TranslationYBy(-1200).SetDuration(500);
                MainFragmentOpened = fragmentView;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void ResetButtonTags()
        {
            try
            {
                StickerButton.Tag = "Closed";
                GiftButton.Tag = "Closed";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
         
        #endregion

        #region Permissions && Result

        //Result
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            try
            {
                base.OnActivityResult(requestCode, resultCode, data);
                if (requestCode == 108 || requestCode == CropImage.CropImageActivityRequestCode)
                {
                    if (IMethods.CheckConnectivity())
                    {
                        var result = CropImage.GetActivityResult(data);
                        if (result.IsSuccessful)
                        {
                            var resultPathImage = result.Uri.Path;
                            if (!string.IsNullOrEmpty(resultPathImage))
                            {
                                UnixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                var time2 = UnixTimestamp.ToString();
                                string timeNow = DateTime.Now.ToString("hh:mm");

                                //Sent image 
                                if (IMethods.CheckConnectivity())
                                {
                                    GetChatConversationsObject.Messages message = new GetChatConversationsObject.Messages
                                    {
                                        Id = UnixTimestamp,
                                        FromName = UserDetails.FullName,
                                        FromAvater = UserDetails.Avatar,
                                        ToName = UserInfoData?.Fullname ?? "",
                                        ToAvater = UserInfoData?.Avater ?? "",
                                        From = UserDetails.UserId,
                                        To = Userid,
                                        Text = "",
                                        Media = resultPathImage,
                                        FromDelete = 0,
                                        ToDelete = 0,
                                        Sticker = "",
                                        CreatedAt = timeNow,
                                        Seen = 0,
                                        Type = "Sent",
                                        MessageType = "media"
                                    };

                                    int index = MAdapter.MessageList.IndexOf(MAdapter.MessageList.Last());
                                    if (index > -1)
                                    {
                                        MAdapter.MessageList.Add(message);
                                        MAdapter.NotifyItemInserted(index);

                                        //Scroll Down >> 
                                        ChatBoxRecyclerView.ScrollToPosition(index);
                                    }
                                     
                                    MessageController.SendMessageTask(Userid, "", "", resultPathImage, time2, UserInfoData).ConfigureAwait(false);
                                }
                                else
                                {
                                    Toast.MakeText(this, GetString(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short);
                                }

                                EmojisIconEditTextView.Text = "";

                            }
                        }
                        else
                        {
                            Toast.MakeText(this, GetText(Resource.String.Lbl_something_went_wrong), ToastLength.Long).Show();
                        }
                    }
                    else
                    {
                        Toast.MakeText(this, GetText(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Long).Show();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //Permissions
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            try
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

                if (requestCode == 108)
                {
                    if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    {
                        OpenDialogGallery();
                    }
                    else
                    {
                        Toast.MakeText(this, GetText(Resource.String.Lbl_Permission_is_denied), ToastLength.Long).Show();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        #endregion

        public override void OnBackPressed()
        {
            try
            {
                int count = MAdapter.GetSelectedItemCount();
                if (SupportFragmentManager.BackStackEntryCount > 0)
                {
                    SupportFragmentManager.PopBackStack();
                    ResetButtonTags();
                    StickerButton.Drawable.SetTint(Color.ParseColor("#888888"));
                    GiftButton.Drawable.SetTint(Color.ParseColor("#888888"));

                    if (SupportFragmentManager.Fragments.Count > 0)
                    {
                        FragmentTransaction fragmentManager = SupportFragmentManager.BeginTransaction();
                        foreach (var vrg in SupportFragmentManager.Fragments)
                        {
                            if (SupportFragmentManager.Fragments.Contains(ChatStickersFragment))
                            {
                                fragmentManager.Remove(ChatStickersFragment);
                            }
                            else if (SupportFragmentManager.Fragments.Contains(ChatGiftFragment))
                            {
                                fragmentManager.Remove(ChatGiftFragment);
                            }
                        } 
                        fragmentManager.Commit();
                    }
                }
                else if (count > 0)
                {
                    if (Timer != null)
                    {
                        Timer.Enabled = true;
                        Timer.Start();
                    }

                    TopChatToolBar.Visibility = ViewStates.Visible;
                    ActionMode.Finish();
                }
                else
                {
                    base.OnBackPressed();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e); 
            } 
        }
         
        public void OpenDialogGallery()
        {
            try
            {
                // Check if we're running on Android 5.0 or higher
                if ((int)Build.VERSION.SdkInt < 23)
                {
                    IMethods.IPath.Chack_MyFolder();

                    //Open Image 
                    var myUri = Uri.FromFile(new File(IMethods.IPath.FolderDcimImage, IMethods.GetTimestamp(DateTime.Now) + ".jpeg"));
                    CropImage.Builder()
                        .SetInitialCropWindowPaddingRatio(0)
                        .SetAutoZoomEnabled(true)
                        .SetMaxZoom(4)
                        .SetGuidelines(CropImageView.Guidelines.On)
                        .SetCropMenuCropButtonTitle(GetText(Resource.String.Lbl_Done))
                        .SetOutputUri(myUri).Start(this);
                }
                else
                {
                    if (!CropImage.IsExplicitCameraPermissionRequired(this) && CheckSelfPermission(Manifest.Permission.ReadExternalStorage) == Permission.Granted &&
                        CheckSelfPermission(Manifest.Permission.WriteExternalStorage) == Permission.Granted && CheckSelfPermission(Manifest.Permission.Camera) == Permission.Granted)
                    {
                        IMethods.IPath.Chack_MyFolder();

                        //Open Image 
                        var myUri = Uri.FromFile(new File(IMethods.IPath.FolderDcimImage, IMethods.GetTimestamp(DateTime.Now) + ".jpeg"));
                        CropImage.Builder()
                            .SetInitialCropWindowPaddingRatio(0)
                            .SetAutoZoomEnabled(true)
                            .SetMaxZoom(4)
                            .SetGuidelines(CropImageView.Guidelines.On)
                            .SetCropMenuCropButtonTitle(GetText(Resource.String.Lbl_Done))
                            .SetOutputUri(myUri).Start(this);
                    }
                    else
                    {
                        //request Code 108
                        new PermissionsController(this).RequestPermission(108);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
         
        public void GetStickersGiftsLists()
        {
            try
            {
                var sqlEntity = new SqLiteDatabase();

                var listGifts = sqlEntity.GetGiftsList(); 
                var listStickers = sqlEntity.GetStickersList();

                if (ListUtils.StickersList.Count == 0 && listStickers != null)
                    ListUtils.StickersList = listStickers;
                else
                    ApiRequest.GetStickers().ConfigureAwait(false);

                if (ListUtils.GiftsList.Count == 0 && listGifts != null)
                    ListUtils.GiftsList = listGifts;
                else
                    ApiRequest.GetGifts().ConfigureAwait(false);
                 
                sqlEntity.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}