namespace Narumikazuchi.GroupManager;
/// <summary>
/// Interaction logic for CyclingProgress.xaml
/// </summary>
public partial class CyclingProgress
{
    public CyclingProgress()
    {
        this.InitializeComponent();

        m_AnimationTimer = new(priority: DispatcherPriority.ContextIdle,
                               dispatcher: this.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(75)
        };

        if (this.IsVisible)
        {
            this.Start();
        }
    }
}

// Non-Public
partial class CyclingProgress : UserControl
{
    private void Start()
    {
        Mouse.OverrideCursor = Cursors.Wait;
        m_AnimationTimer.Tick += this.HandleAnimationTick;
        m_AnimationTimer.Start();
    }

    private void Stop()
    {
        m_AnimationTimer.Stop();
        Mouse.OverrideCursor = Cursors.Arrow;
        m_AnimationTimer.Tick -= this.HandleAnimationTick;
    }

    private static void SetLocation(Ellipse ellipse,
                                    Double offset,
                                    Double locOffset,
                                    Double step)
    {
        ellipse.SetValue(dp: Canvas.LeftProperty,
                         value: 50d + Math.Sin(offset + locOffset * step) * 50d);
        ellipse.SetValue(dp: Canvas.TopProperty,
                         value: 50d + Math.Cos(offset + locOffset * step) * 50d);
    }

    private void HandleAnimationTick(Object? sender,
                                     EventArgs eventArgs) =>
        m_SpinnerRotate.Angle = (m_SpinnerRotate.Angle + 36) % 360;

    private void HandleLoaded(Object? sender,
                              RoutedEventArgs eventArgs)
    {
        Double step = Math.PI * 2 / 10;

        SetLocation(ellipse: C0,
                    offset: Math.PI,
                    locOffset: 0.0,
                    step: step);
        SetLocation(ellipse: C1,
                    offset: Math.PI,
                    locOffset: 1.0,
                    step: step);
        SetLocation(ellipse: C2,
                    offset: Math.PI,
                    locOffset: 2.0,
                    step: step);
        SetLocation(ellipse: C3,
                    offset: Math.PI,
                    locOffset: 3.0,
                    step: step);
        SetLocation(ellipse: C4,
                    offset: Math.PI,
                    locOffset: 4.0,
                    step: step);
        SetLocation(ellipse: C5,
                    offset: Math.PI,
                    locOffset: 5.0,
                    step: step);
        SetLocation(ellipse: C6,
                    offset: Math.PI,
                    locOffset: 6.0,
                    step: step);
        SetLocation(ellipse: C7,
                    offset: Math.PI,
                    locOffset: 7.0,
                    step: step);
        SetLocation(ellipse: C8,
                    offset: Math.PI,
                    locOffset: 8.0,
                    step: step);
    }

    private void HandleUnloaded(Object sender,
                                RoutedEventArgs eventArgs) =>
        this.Stop();

    private void HandleVisibleChanged(Object? sender,
                                      DependencyPropertyChangedEventArgs eventArgs)
    {
        Boolean isVisible = Convert.ToBoolean(value: eventArgs.NewValue);

        if (isVisible)
        {
            this.Start();
        }
        else
        {
            this.Stop();
        }
    }

    private readonly DispatcherTimer m_AnimationTimer;
}