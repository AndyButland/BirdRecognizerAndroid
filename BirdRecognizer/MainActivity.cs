namespace BirdRecognizer
{
    using System;
    using System.Threading.Tasks;
    using Android.App;
    using Android.Graphics;
    using Android.OS;
    using Android.Support.V7.App;
    using Android.Util;
    using Android.Views;
    using Android.Widget;
    using Plugin.CurrentActivity;
    using Plugin.Media;
    using Plugin.Media.Abstractions;
    using Plugin.Permissions;
    using Permission = Android.Content.PM.Permission;

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private readonly ImageClassifier _imageClassifier = new ImageClassifier();

        private Button _selectPhotoButton;
        private ImageView _photoView;
        private TextView _resultLabel;
        private ProgressBar _progressBar;

        public Android.Support.V7.Widget.Toolbar Toolbar { get; set; }

        private Button SelectPhotoButton
            => _selectPhotoButton ?? 
                (_selectPhotoButton = FindViewById<Button>(Resource.Id.select_photo_button));
        
        private ImageView PhotoView
            => _photoView ?? 
                (_photoView = FindViewById<ImageView>(Resource.Id.photo));
        
        private TextView ResultLabel
            => _resultLabel ?? 
                (_resultLabel = FindViewById<TextView>(Resource.Id.result_label));
        
        private ProgressBar ProgressBar
            => _progressBar ?? 
                (_progressBar = FindViewById<ProgressBar>(Resource.Id.progressbar));
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            CrossCurrentActivity.Current.Activity = this;

            SetContentView(Resource.Layout.main);

            Toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            if (Toolbar != null)
            {
                SetSupportActionBar(Toolbar);
            }

            SupportActionBar.SetDisplayHomeAsUpEnabled(false);
            SupportActionBar.SetHomeButtonEnabled(false);

            SelectPhotoButton.Click += TakePhotoButtonClick;
        }

        private async void TakePhotoButtonClick(object sender, EventArgs e)
        {
            if (!CrossMedia.Current.IsTakePhotoSupported)
            {
                Toast.MakeText(ApplicationContext, 
                    "Cannot select photos from the device.", ToastLength.Long).Show();
                return;
            }

            SelectPhotoButton.Enabled = false;
            ProgressBar.Visibility = ViewStates.Visible;

            try
            {
                var image = await CrossMedia.Current.PickPhotoAsync(
                    new PickMediaOptions { PhotoSize = PhotoSize.Medium });
                var bitmap = await BitmapFactory.DecodeStreamAsync(
                    image.GetStreamWithImageRotatedForExternalStorage());

                PhotoView.SetImageBitmap(bitmap);

                var result = await Task.Run(() => _imageClassifier.RecognizeImage(bitmap));
                ResultLabel.Text = result.ToString();
            }
            catch (Exception ex)
            {
                Log.Error("BirdRecognizer", ex.ToString());
            }
            finally
            {
                SelectPhotoButton.Enabled = true;
                ProgressBar.Visibility = ViewStates.Invisible;
            }
        }

        public override void OnRequestPermissionsResult(
            int requestCode, string[] permissions, Permission[] grantResults)
            => PermissionsImplementation.Current.OnRequestPermissionsResult(
                requestCode, permissions, grantResults);
    }
}
