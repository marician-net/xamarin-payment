﻿using System;
using System.Collections.Generic;
using System.Linq;
using AFollestad.MaterialDialogs;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using AndroidHUD;
using Java.Lang;
using QuickDate.Helpers.Controller;
using QuickDate.Helpers.Fonts;
using QuickDate.Helpers.Model;
using QuickDate.Helpers.Utils;
using QuickDate.SQLite;
using QuickDateClient.Classes.Global;
using QuickDateClient.Classes.Users;
using QuickDateClient.Requests;
using Exception = System.Exception;

namespace QuickDate.Activities.MyProfile
{
    [Activity(Icon = "@drawable/icon", Theme = "@style/MyTheme", ConfigurationChanges = ConfigChanges.Locale | ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class LooksEditActivity : AppCompatActivity, MaterialDialog.IListCallback, MaterialDialog.ISingleButtonCallback
    {
        #region Variables Basic

        public TextView BackIcon, EthnicityIcon, BodyIcon, HeightIcon, HairColorIcon;
        public EditText EdtEthnicity, EdtBody, EdtHeight, EdtHairColor;
        public Button BtnSave;
        public string TypeDialog;
        public int IdEthnicity, IdBody, IdHairColor;
        public AdView MAdView;

        #endregion

        #region General

        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);

                IMethods.IApp.FullScreenApp(this);

                // Create your application here
                SetContentView(Resource.Layout.ButtomSheetLooksEdit);

                //Get Value And Set Toolbar
                InitComponent();
                InitAdView();

                GetMyInfoData();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected override void OnResume()
        {
            try
            {
                base.OnResume();
                AddOrRemoveEvent(true);
                MAdView?.Resume();
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
                MAdView?.Pause();
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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void OnLowMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration);
                base.OnLowMemory();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region Functions

        public void InitComponent()
        {
            try
            {
                BackIcon = FindViewById<TextView>(Resource.Id.IconBack);

                EthnicityIcon = FindViewById<TextView>(Resource.Id.IconEthnicity);
                EdtEthnicity = FindViewById<EditText>(Resource.Id.EthnicityEditText);

                BodyIcon = FindViewById<TextView>(Resource.Id.IconBody);
                EdtBody = FindViewById<EditText>(Resource.Id.BodyEditText);

                HeightIcon = FindViewById<TextView>(Resource.Id.IconHeight);
                EdtHeight = FindViewById<EditText>(Resource.Id.HeightEditText);

                HairColorIcon = FindViewById<TextView>(Resource.Id.IconHairColor);
                EdtHairColor = FindViewById<EditText>(Resource.Id.HairColorEditText);
                 
                BtnSave = FindViewById<Button>(Resource.Id.ApplyButton);

                FontUtils.SetTextViewIcon(FontsIconFrameWork.IonIcons, BackIcon, IonIconsFonts.ChevronLeft);
                FontUtils.SetTextViewIcon(FontsIconFrameWork.FontAwesomeRegular, EthnicityIcon, FontAwesomeIcon.TheaterMasks);
                FontUtils.SetTextViewIcon(FontsIconFrameWork.FontAwesomeLight, BodyIcon, FontAwesomeIcon.Male);
                FontUtils.SetTextViewIcon(FontsIconFrameWork.FontAwesomeLight, HeightIcon, FontAwesomeIcon.TextHeight);
                FontUtils.SetTextViewIcon(FontsIconFrameWork.FontAwesomeRegular, HairColorIcon, FontAwesomeIcon.Cannabis);
                 
                EdtEthnicity.SetFocusable(ViewFocusability.NotFocusable);
                EdtBody.SetFocusable(ViewFocusability.NotFocusable); 
                EdtHairColor.SetFocusable(ViewFocusability.NotFocusable);
                EdtHeight.SetFocusable(ViewFocusability.NotFocusable);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void InitAdView()
        {
            try
            {
                MAdView = FindViewById<AdView>(Resource.Id.adView);
                var dataUser = ListUtils.MyUserInfo.FirstOrDefault(a => a.Id == UserDetails.UserId);
                if (dataUser?.IsPro == "0")
                {
                    if (AppSettings.ShowAdmobBanner)
                    {
                        MAdView.Visibility = ViewStates.Visible;
                        var adRequest = new AdRequest.Builder().Build();
                        MAdView.LoadAd(adRequest);
                    }
                    else
                    {
                        MAdView.Pause();
                        MAdView.Visibility = ViewStates.Gone;
                    }
                }
                else
                {
                    MAdView.Pause();
                    MAdView.Visibility = ViewStates.Gone;
                }
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
                    BackIcon.Click += BackIconOnClick;
                    BtnSave.Click += BtnSaveOnClick;
                    EdtEthnicity.Click += EdtEthnicityOnClick;
                    EdtBody.Click += EdtBodyOnClick;
                    EdtHairColor.Click += EdtHairColorOnClick;
                    EdtHeight.Click += EdtHeightOnClick;
                }
                else
                {
                    BackIcon.Click -= BackIconOnClick;
                    BtnSave.Click -= BtnSaveOnClick;
                    EdtEthnicity.Click -= EdtEthnicityOnClick;
                    EdtBody.Click -= EdtBodyOnClick;
                    EdtHairColor.Click -= EdtHairColorOnClick;
                    EdtHeight.Click -= EdtHeightOnClick;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
         
        #endregion

        #region Events
         
        //HairColor
        private void EdtHairColorOnClick(object sender, EventArgs e)
        {
            try
            {
                TypeDialog = "HairColor";
                string[] hairColorArray = Application.Context.Resources.GetStringArray(Resource.Array.HairColorArray);

                var arrayAdapter = new List<string>();
                var dialogList = new MaterialDialog.Builder(this);

                foreach (var item in hairColorArray)
                    arrayAdapter.Add(item);

                dialogList.Title(GetText(Resource.String.Lbl_ChooseHairColor));
                dialogList.Items(arrayAdapter);
                dialogList.PositiveText(GetText(Resource.String.Lbl_Close)).OnPositive(this);
                dialogList.AlwaysCallSingleChoiceCallback();
                dialogList.ItemsCallback(this).Build().Show();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
         
        //Body
        private void EdtBodyOnClick(object sender, EventArgs e)
        {
            try
            {
                TypeDialog = "Body";
                string[] bodyArray = Application.Context.Resources.GetStringArray(Resource.Array.BodyArray);

                var arrayAdapter = new List<string>();
                var dialogList = new MaterialDialog.Builder(this);

                foreach (var item in bodyArray)
                    arrayAdapter.Add(item);

                dialogList.Title(GetText(Resource.String.Lbl_BodyType));
                dialogList.Items(arrayAdapter);
                dialogList.PositiveText(GetText(Resource.String.Lbl_Close)).OnPositive(this);
                dialogList.AlwaysCallSingleChoiceCallback();
                dialogList.ItemsCallback(this).Build().Show();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        //Ethnicity
        private void EdtEthnicityOnClick(object sender, EventArgs e)
        {
            try
            {
                TypeDialog = "Ethnicity";
                string[] ethnicityArray = Application.Context.Resources.GetStringArray(Resource.Array.EthnicityArray);

                var arrayAdapter = new List<string>();
                var dialogList = new MaterialDialog.Builder(this);

                foreach (var item in ethnicityArray)
                    arrayAdapter.Add(item);

                dialogList.Title(GetText(Resource.String.Lbl_BodyType));
                dialogList.Items(arrayAdapter);
                dialogList.PositiveText(GetText(Resource.String.Lbl_Close)).OnPositive(this);
                dialogList.AlwaysCallSingleChoiceCallback();
                dialogList.ItemsCallback(this).Build().Show();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void EdtHeightOnClick(object sender, EventArgs e)
        {
            try
            {
                TypeDialog = "Height";
                string[] heightArray = Application.Context.Resources.GetStringArray(Resource.Array.HeightArray);

                var arrayAdapter = new List<string>();
                var dialogList = new MaterialDialog.Builder(this);

                foreach (var item in heightArray)
                    arrayAdapter.Add(item);

                dialogList.Title(GetText(Resource.String.Lbl_Height));
                dialogList.Items(arrayAdapter);
                dialogList.PositiveText(GetText(Resource.String.Lbl_Close)).OnPositive(this);
                dialogList.AlwaysCallSingleChoiceCallback();
                dialogList.ItemsCallback(this).Build().Show();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }


        //Click save data and sent api
        private async void BtnSaveOnClick(object sender, EventArgs e)
        {
            try
            {
                if (IMethods.CheckConnectivity())
                {
                    //Show a progress
                    AndHUD.Shared.Show(this, GetText(Resource.String.Lbl_Loading));

                    var dictionary = new Dictionary<string, string>
                    {
                        {"ethnicity", IdEthnicity.ToString()},  
                        {"body", IdBody.ToString()}, 
                        {"height", EdtHeight.Text.Replace("cm","").Replace(" ","")},
                        {"hair_color",IdHairColor.ToString()}, 
                    };

                    (int apiStatus, var respond) = await RequestsAsync.Users.UpdateProfileAsync(dictionary);
                    if (apiStatus == 200)
                    {
                        if (respond is UpdateProfileObject result)
                        {
                            var local = ListUtils.MyUserInfo.FirstOrDefault(a => a.Id == UserDetails.UserId);
                            if (local != null)
                            {
                                local.Ethnicity = IdEthnicity;
                                local.Body = IdBody;
                                local.Height = EdtHeight.Text.Replace("cm", "").Replace(" ", "");
                                local.HairColor = IdHairColor.ToString();
                                 
                                SqLiteDatabase database = new SqLiteDatabase();
                                database.InsertOrUpdate_DataMyInfo(local);
                                database.Dispose();
                            }

                            Toast.MakeText(this, GetText(Resource.String.Lbl_Done), ToastLength.Short).Show();
                            AndHUD.Shared.Dismiss(this);
                             
                            Intent resultIntent = new Intent();
                            SetResult(Result.Ok, resultIntent);
                            Finish();
                        }
                    }
                    else if (apiStatus == 400)
                    {
                        if (respond is ErrorObject error)
                        {
                            var errorText = error.ErrorData.ErrorText;
                            AndHUD.Shared.ShowError(this, errorText, MaskType.Clear, TimeSpan.FromSeconds(2));

                            if (errorText.Contains("Permission Denied"))
                                ApiRequest.Logout(this);
                        }
                    }
                    else if (apiStatus == 404)
                    {
                        var error = respond.ToString();
                        //Toast.MakeText(this, error, ToastLength.Short).Show();
                    }
                    
                    AndHUD.Shared.Dismiss(this);
                }
                else
                {
                    Toast.MakeText(this, GetText(Resource.String.Lbl_CheckYourInternetConnection), ToastLength.Short).Show();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                AndHUD.Shared.Dismiss(this);
            }
        }

        private void BackIconOnClick(object sender, EventArgs e)
        {
            try
            {
                Intent resultIntent = new Intent();
                SetResult(Result.Canceled, resultIntent);
                Finish();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion

        public void GetMyInfoData()
        {
            try
            {
                if (ListUtils.MyUserInfo.Count == 0)
                {
                    var sqlEntity = new SqLiteDatabase();
                    sqlEntity.GetDataMyInfo();
                    sqlEntity.Dispose();
                }

                var dataUser = ListUtils.MyUserInfo.FirstOrDefault(a => a.Id == UserDetails.UserId);
                if (dataUser != null)
                {
                    string ethnicity = QuickDateTools.GetEthnicity(Convert.ToInt32(dataUser.Ethnicity));
                    if (IMethods.Fun_String.StringNullRemover(ethnicity) != "-----")
                    {
                        EdtEthnicity.Text = ethnicity;
                        IdEthnicity = Convert.ToInt32(dataUser.Ethnicity);
                    }

                    string body = QuickDateTools.GetBody(Convert.ToInt32(dataUser.Body));
                    if (IMethods.Fun_String.StringNullRemover(body) != "-----")
                    {
                        EdtBody.Text = body;
                        IdBody = Convert.ToInt32(dataUser.Body);
                    }

                    EdtHeight.Text = dataUser.Height + " "+ GetText(Resource.String.Lbl_Cm);
                     
                    string hairColor = QuickDateTools.GetHairColor(Convert.ToInt32(dataUser.HairColor));
                    if (IMethods.Fun_String.StringNullRemover(hairColor) != "-----")
                    {
                        EdtHairColor.Text = hairColor;
                        IdHairColor = Convert.ToInt32(dataUser.HairColor);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #region MaterialDialog

        public void OnSelection(MaterialDialog p0, View p1, int itemId, ICharSequence itemString)
        {
            try
            {
                if (TypeDialog == "Ethnicity")
                {
                    IdEthnicity = itemId + 1;
                    EdtEthnicity.Text = itemString.ToString();
                }
                else if (TypeDialog == "Body")
                {
                    IdBody = itemId + 1;
                    EdtBody.Text = itemString.ToString();
                } 
                else if (TypeDialog == "HairColor")
                {
                    IdHairColor = itemId + 1;
                    EdtHairColor.Text = itemString.ToString();
                }
                else if (TypeDialog == "Height")
                { 
                    EdtHeight.Text = itemString.ToString() + " " + GetText(Resource.String.Lbl_Cm);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void OnClick(MaterialDialog p0, DialogAction p1)
        {
            try
            {
                if (p1 == DialogAction.Positive)
                {
                }
                else if (p1 == DialogAction.Negative)
                {
                    p0.Dismiss();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

    }
}