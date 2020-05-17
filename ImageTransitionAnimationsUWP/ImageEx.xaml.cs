using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace ImageTransitionAnimationsUWP
{
  public sealed partial class ImageEx : UserControl, INotifyPropertyChanged
  {
    private const Stretch DEFAULT_STRETCH = Stretch.Uniform;

    bool isFrontVisible = true;

    private Array animationTypes = Enum.GetValues(typeof(AnimationType));
    private Random random = new Random();

    private Compositor compositor;
    private Visual visualB;
    private Visual visualF;
    private Animations animations;

    private readonly Vector3 vZero = new Vector3(0f, 0f, 0f);
    private readonly Vector3 vOne = new Vector3(1f, 1f, 1f);

    private Image newImage;
    private Image oldImage;

    private Visual newVisual;
    private Visual oldVisual;

    public ImageEx()
    {
      InitializeComponent();

      compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
      visualB = ElementCompositionPreview.GetElementVisual(imageBackContainer);
      visualF = ElementCompositionPreview.GetElementVisual(imageFrontContainer);
      animations = new Animations(compositor);

      AnimationType = AnimationType.Random;

      HorizontalImageStretch = DEFAULT_STRETCH;
      VerticalImageStretch = DEFAULT_STRETCH;
    }

    public AnimationType AnimationType
    {
      get { return (AnimationType)GetValue(AnimationTypeProperty); }
      set
      {
        SetValue(AnimationTypeProperty, value);
        OnAnimationTypeChanged(value);
      }
    }

    public static readonly DependencyProperty AnimationTypeProperty =
        DependencyProperty.Register("AnimationType", typeof(AnimationType), typeof(ImageEx), new PropertyMetadata(AnimationType.Opacity));

    private void OnAnimationTypeChanged(AnimationType value)
    {
      
    }

    public Direction Direction
    {
      get { return (Direction)GetValue(DirectionProperty); }
      set { SetValue(DirectionProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Direction.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register("Direction", typeof(Direction), typeof(ImageEx), new PropertyMetadata(Direction.Next));

    public int DecodePixelHeight
    {
      get { return (int)GetValue(DecodePixelHeightProperty); }
      set { SetValue(DecodePixelHeightProperty, value); }
    }

    public static readonly DependencyProperty DecodePixelHeightProperty =
        DependencyProperty.Register("DecodePixelHeight", typeof(int), typeof(int), null);

    public int DecodePixelWidth
    {
      get { return (int)GetValue(DecodePixelWidthProperty); }
      set { SetValue(DecodePixelWidthProperty, value); }
    }

    public static readonly DependencyProperty DecodePixelWidthProperty =
        DependencyProperty.Register("DecodePixelWidth", typeof(int), typeof(int), null);

    public TimeSpan Duration
    {
      get { return (TimeSpan)GetValue(DurationProperty); }
      set
      {
        SetValue(DurationProperty, value);
      }
    }

    // Using a DependencyProperty as the backing store for FadeOutDuration.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(ImageEx), new PropertyMetadata(TimeSpan.FromSeconds(1)));

    public Uri ImageUri
    {
      get { return (Uri)GetValue(ImageUriProperty); }
      set
      {
        SetValue(ImageUriProperty, value);
        OnImageUriChange();
      }
    }

    public static readonly DependencyProperty ImageUriProperty =
        DependencyProperty.Register("ImageUri", typeof(Uri), typeof(ImageEx), null);

    private void OnImageUriChange()
    {
      Source = GetBitmapImage(ImageUri);
    }

    // Rendering a StorageFile from LocalCache doesn't work via uri (it seems that not everything likes the 
    // ms-appdata:///localcache schema) so we should also support setting the BitmapImage directly
    public BitmapSource Source
    {
      get { return (BitmapSource)GetValue(SourceProperty); }
      set
      {
        SetValue(SourceProperty, value);
      }
    }

    private static readonly DependencyProperty SourceProperty =
      DependencyProperty.Register("Source", typeof(BitmapSource), typeof(ImageEx), new PropertyMetadata(null, new PropertyChangedCallback(OnSourceChanged)));

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var imageEx = d as ImageEx;

      if (imageEx != null)
      {
        if (imageEx.Source.PixelHeight > imageEx.Source.PixelWidth)
        {
          imageEx.Stretch = imageEx.VerticalImageStretch;
        }
        else
        {
          imageEx.Stretch = imageEx.HorizontalImageStretch;
        }

        Debug.WriteLine($"ImageEx: Source Changed & Stretch set to {imageEx.Stretch}");

        imageEx.Animate();
      }
    }

    private Stretch _stretch = DEFAULT_STRETCH;
    public Stretch Stretch
    {
      get { return _stretch; }
      set
      {
        _stretch = value;
        OnPropertyChanged();
      }
    }

    public Stretch HorizontalImageStretch
    {
      get { return (Stretch)GetValue(HorizontalImageStretchProperty); }
      set
      {
        SetValue(HorizontalImageStretchProperty, value);
      }
    }

    public static readonly DependencyProperty HorizontalImageStretchProperty =
        DependencyProperty.Register("HorizontalImageStretch", typeof(Stretch), typeof(ImageEx), null);

    public Stretch VerticalImageStretch
    {
      get { return (Stretch)GetValue(VerticalImageStretchProperty); }
      set
      {
        SetValue(VerticalImageStretchProperty, value);
      }
    }

    public static readonly DependencyProperty VerticalImageStretchProperty =
        DependencyProperty.Register("VerticalImageStretch", typeof(Stretch), typeof(ImageEx), null);

    private void Animate()
    {
      Animate(AnimationType);
    }

    private void Animate(AnimationType animationType)
    {
      Debug.WriteLine($"   -> Animate: {animationType}");

      // Avoid each animation having to duplicate code for depending on which image is visible
      // by setting up these references that they can use to determine which image is coming in (new)
      // and which is leaving (old)
      if (isFrontVisible)
      {

        oldImage = imageFront;
        oldVisual = visualF;

        newImage = imageBack;
        newVisual = visualB;
      }
      else
      {
        oldImage = imageBack;
        oldVisual = visualB;

        newImage = imageFront;
        newVisual = visualF;
      }

      visualF.Opacity = 1;
      visualB.Opacity = 1;

      // Apply Stretch here, only for the active control. Otherwise, if new Stretch values get
      // applied to both images there is a jump in the visible image (if the Stretch value changes)
      newImage.Stretch = Stretch;

      switch (animationType)
      {
        case AnimationType.Opacity:
          OpacityAnimation();
          break;
        case AnimationType.OpacitySpring:
          OpacitySpringAnimation();
          break;
        case AnimationType.ScaleAndOpacity:
          ScaleAndOpacityAnimation();
          break;
        case AnimationType.SlideHorizontally:
          SlideAnimation(true);
          break;
        case AnimationType.SlideVertically:
          SlideAnimation(false);
          break;
        case AnimationType.StackFromLeft:
          StackHorizontalAnimation(true, true);
          break;
        case AnimationType.StackFromRight:
          StackHorizontalAnimation(true, false);
          break;
        case AnimationType.StackFromTop:
          StackHorizontalAnimation(false, true);
          break;
        case AnimationType.StackFromBottom:
          StackHorizontalAnimation(false, false);
          break;
        case AnimationType.StackAndScaleFromLeft:
          StackAndScaleAnimation(true, true);
          break;
        case AnimationType.StackAndScaleFromRight:
          StackAndScaleAnimation(true, false);
          break;
        case AnimationType.StackAndScaleFromTop:
          StackAndScaleAnimation(false, true);
          break;
        case AnimationType.StackAndScaleFromBottom:
          StackAndScaleAnimation(false, false);
          break;
        case AnimationType.Random:
          AnimationType randomAnimation;

          do
          {
            randomAnimation = (AnimationType)animationTypes.GetValue(random.Next(animationTypes.Length));
          } while (randomAnimation == AnimationType.Random);

          Animate(randomAnimation);
          return;

        default:
          break;
      }

      isFrontVisible = !isFrontVisible;
    }

    private void OpacityAnimation()
    {
      newImage.Source = Source;

      oldVisual.Offset = new Vector3(0f, 0f, 0f);
      newVisual.Offset = new Vector3(0f, 0f, 0f);

      oldVisual.StartAnimation("Opacity", animations.CreateOpacityAnimation(1f, 0f, Duration));
      newVisual.StartAnimation("Opacity", animations.CreateOpacityAnimation(0f, 1f, Duration));
    }

    private void OpacitySpringAnimation()
    {
      newImage.Source = Source;

      oldVisual.Offset = new Vector3(0f, 0f, 0f);
      newVisual.Offset = new Vector3(0f, 0f, 0f);

      oldVisual.StartAnimation("Opacity", animations.CreateOpacitySpringAnimation(1f, 0f, Duration));
      newVisual.StartAnimation("Opacity", animations.CreateOpacitySpringAnimation(0f, 1f, Duration));
    }

    private void SlideAnimation(bool horizontally)
    {
      Vector3 leftTop;
      Vector3 rightBottom;
      if (horizontally)
      {
        leftTop = new Vector3((int)ActualWidth, 0f, 0f);
        rightBottom = new Vector3(-(int)ActualWidth, 0f, 0f);
      }
      else
      {
        leftTop = new Vector3(0f, (int)ActualHeight, 0f);
        rightBottom = new Vector3(0f, -(int)ActualHeight, 0f);
      }
      if (isFrontVisible)
      {
        imageBack.Source = Source;
      }
      else
      {
        imageFront.Source = Source;
      }
      if (Direction == Direction.Next)
      {
        if (isFrontVisible)
        {
          visualB.StartAnimation(nameof(visualB.Offset), animations.CreateSlideAnimation(leftTop, vZero, Duration));
          visualF.StartAnimation(nameof(visualF.Offset), animations.CreateSlideAnimation(vZero, rightBottom, Duration));
        }
        else
        {
          visualB.StartAnimation(nameof(visualB.Offset), animations.CreateSlideAnimation(vZero, rightBottom, Duration));
          visualF.StartAnimation(nameof(visualF.Offset), animations.CreateSlideAnimation(leftTop, vZero, Duration));
        }
      }
      else
      {
        if (isFrontVisible)
        {
          visualB.StartAnimation(nameof(visualB.Offset), animations.CreateSlideAnimation(rightBottom, vZero, Duration));
          visualF.StartAnimation(nameof(visualF.Offset), animations.CreateSlideAnimation(vZero, leftTop, Duration));
        }
        else
        {
          visualB.StartAnimation(nameof(visualB.Offset), animations.CreateSlideAnimation(vZero, leftTop, Duration));
          visualF.StartAnimation(nameof(visualF.Offset), animations.CreateSlideAnimation(rightBottom, vZero, Duration));
        }
      }
    }

    private void StackHorizontalAnimation(bool horizontally, bool fromLeftTop)
    {
      int dir = fromLeftTop ? 1 : -1;

      Vector3 oldImageEndVector;
      Vector3 newImageStartVector;
      
      newImage.Source = Source;

      if (horizontally)
      {
        oldImageEndVector = new Vector3(dir * (int)ActualWidth, 0f, 0f);
        newImageStartVector = new Vector3(-1 * dir * (int)ActualWidth, 0f, 0f); 
      }
      else
      {
        oldImageEndVector = new Vector3(0f, dir * (int)ActualHeight, 0f);
        newImageStartVector = new Vector3(0f, -1 * dir * (int)ActualHeight, 0f);
      }

      // Not currently using Previous (just animate in reverse?)
      if (Direction == Direction.Next)
      {
        newImage.SetValue(Canvas.ZIndexProperty, 0);
        oldImage.SetValue(Canvas.ZIndexProperty, 1);

        oldVisual.StartAnimation(nameof(oldVisual.Offset), animations.CreateSlideAnimation(vZero, oldImageEndVector, Duration));
        newVisual.StartAnimation(nameof(oldVisual.Offset), animations.CreateSlideAnimation(newImageStartVector, vZero, Duration));
      }
    }

    private void StackAndScaleAnimation(bool horizontally, bool fromLeftTop)
    {
      CompositionAnimationGroup animationGroupB = compositor.CreateAnimationGroup();
      CompositionAnimationGroup animationGroupF = compositor.CreateAnimationGroup();

      visualB.CenterPoint = new Vector3((visualB.Size.X / 2.0f), (visualB.Size.Y / 2.0f), 0.0f);
      visualF.CenterPoint = new Vector3((visualF.Size.X / 2.0f), (visualF.Size.Y / 2.0f), 0.0f);

      Vector3 small = new Vector3(0.7f, 0.7f, 7f);
      Vector3 big = new Vector3(1.4f, 1.4f, 1f);

      int dir = fromLeftTop ? -1 : 1;
      if (isFrontVisible)
      {
        imageBack.Source = Source;
        visualB.Offset = new Vector3(0f, 0f, 0f);
      }
      else
      {
        imageFront.Source = Source;
        visualF.Offset = new Vector3(0f, 0f, 0f);
      }
      Vector3 vector;
      if (horizontally)
      {
        vector = new Vector3(dir * (int)ActualWidth, 0f, 0f);
      }
      else
      {
        vector = new Vector3(0f, dir * (int)ActualHeight, 0f);
      }

      if (Direction == Direction.Next)
      {
        if (isFrontVisible)
        {
          imageBack.SetValue(Canvas.ZIndexProperty, 0);
          imageFront.SetValue(Canvas.ZIndexProperty, 1);

          animationGroupB.Add(animations.CreateOpacityAnimation(0.5f, 1f, Duration));
          animationGroupB.Add(animations.CreateScaleAnimation(small, vOne, Duration));

          animationGroupF.Add(animations.CreateSlideAnimation(vZero, vector, Duration));

          visualB.StartAnimationGroup(animationGroupB);
          visualF.StartAnimationGroup(animationGroupF);
        }
        else
        {
          imageBack.SetValue(Canvas.ZIndexProperty, 1);
          imageFront.SetValue(Canvas.ZIndexProperty, 0);

          animationGroupF.Add(animations.CreateOpacityAnimation(0.5f, 1f, Duration));
          animationGroupF.Add(animations.CreateScaleAnimation(small, vOne, Duration));

          animationGroupB.Add(animations.CreateSlideAnimation(vZero, vector, Duration));

          visualB.StartAnimationGroup(animationGroupB);
          visualF.StartAnimationGroup(animationGroupF);
        }
      }
      else
      {
        if (isFrontVisible)
        {
          imageBack.SetValue(Canvas.ZIndexProperty, 1);
          imageFront.SetValue(Canvas.ZIndexProperty, 0);

          visualB.Opacity = 1;
          visualB.Scale = vOne;
          animationGroupB.Add(animations.CreateSlideAnimation(vector, vZero, Duration));

          animationGroupF.Add(animations.CreateOpacityAnimation(1f, 0.5f, Duration));
          animationGroupF.Add(animations.CreateScaleAnimation(vOne, small, Duration));

          visualB.StartAnimationGroup(animationGroupB);
          visualF.StartAnimationGroup(animationGroupF);
        }
        else
        {
          imageBack.SetValue(Canvas.ZIndexProperty, 0);
          imageFront.SetValue(Canvas.ZIndexProperty, 1);

          visualF.Opacity = 1;
          visualF.Scale = vOne;
          animationGroupF.Add(animations.CreateSlideAnimation(vector, vZero, Duration));

          animationGroupB.Add(animations.CreateOpacityAnimation(1f, 0.5f, Duration));
          animationGroupB.Add(animations.CreateScaleAnimation(vOne, small, Duration));

          visualB.StartAnimationGroup(animationGroupB);
          visualF.StartAnimationGroup(animationGroupF);
        }
      }
    }

    private void ScaleAndOpacityAnimation()
    {
      CompositionAnimationGroup animationGroupB = compositor.CreateAnimationGroup();
      CompositionAnimationGroup animationGroupF = compositor.CreateAnimationGroup();

      visualB.CenterPoint = new Vector3((visualB.Size.X / 2.0f), (visualB.Size.Y / 2.0f), 0.0f);
      visualF.CenterPoint = new Vector3((visualF.Size.X / 2.0f), (visualF.Size.Y / 2.0f), 0.0f);

      oldVisual.Offset = new Vector3(0f, 0f, 0f);
      newVisual.Offset = new Vector3(0f, 0f, 0f);

      Vector3 small = new Vector3(0.7f, 0.7f, 7f);
      Vector3 big = new Vector3(1.4f, 1.4f, 1f);

      if (Direction == Direction.Next)
      {
        if (isFrontVisible)
        {
          imageBack.Source = Source;

          animationGroupB.Add(animations.CreateOpacityAnimation(0f, 1f, Duration));
          animationGroupB.Add(animations.CreateScaleAnimation(small, vOne, Duration));
          animationGroupF.Add(animations.CreateOpacityAnimation(1f, 0f, Duration));
          animationGroupF.Add(animations.CreateScaleAnimation(vOne, big, Duration));

          visualB.StartAnimationGroup(animationGroupB);
          visualF.StartAnimationGroup(animationGroupF);
        }
        else
        {
          imageFront.Source = Source;

          animationGroupF.Add(animations.CreateOpacityAnimation(0f, 1f, Duration));
          animationGroupF.Add(animations.CreateScaleAnimation(small, vOne, Duration));
          animationGroupB.Add(animations.CreateOpacityAnimation(1f, 0f, Duration));
          animationGroupB.Add(animations.CreateScaleAnimation(vOne, big, Duration));

          visualB.StartAnimationGroup(animationGroupB);
          visualF.StartAnimationGroup(animationGroupF);
        }
      }
      else
      {
        if (isFrontVisible)
        {
          imageBack.Source = Source;

          animationGroupB.Add(animations.CreateOpacityAnimation(0f, 1f, Duration));
          animationGroupB.Add(animations.CreateScaleAnimation(big, vOne, Duration));
          animationGroupF.Add(animations.CreateOpacityAnimation(1f, 0f, Duration));
          animationGroupF.Add(animations.CreateScaleAnimation(vOne, small, Duration));

          visualB.StartAnimationGroup(animationGroupB);
          visualF.StartAnimationGroup(animationGroupF);
        }
        else
        {
          imageFront.Source = Source;

          animationGroupF.Add(animations.CreateOpacityAnimation(0f, 1f, Duration));
          animationGroupF.Add(animations.CreateScaleAnimation(big, vOne, Duration));
          animationGroupB.Add(animations.CreateOpacityAnimation(1f, 0f, Duration));
          animationGroupB.Add(animations.CreateScaleAnimation(vOne, small, Duration));

          visualB.StartAnimationGroup(animationGroupB);
          visualF.StartAnimationGroup(animationGroupF);
        }
      }
    }

    private BitmapImage GetBitmapImage(Uri uri)
    {
      var bmp = new BitmapImage();
      bmp.DecodePixelHeight = DecodePixelHeight;
      bmp.DecodePixelWidth = DecodePixelWidth;
      bmp.DecodePixelType = DecodePixelType.Logical;
      bmp.UriSource = uri;
      return bmp;
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
  }
}
