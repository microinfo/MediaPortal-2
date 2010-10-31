#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public enum TileMode
  {
    // No tiling
    None,
    // Content is tiled
    Tile,
    // Content is tiled and flipped around x-axis
    FlipX,
    // Content is tiled and flipped around y-axis
    FlipY,
    // Content is tiled and flipped around both x and y-axis
    FlipXY
  };

  public abstract class TileBrush : Brush
  {
    #region Consts

    protected const string EFFECT_TILE = "tile";
    protected const string EFFECT_TILE_OPACITY = "tileopacity";
    protected const string EFFECT_TILE_SIMPLE = "tilesimple";
    protected const string EFFECT_TILE_OPACITY_SIMPLE = "tilesimpleopacity";

    protected const string PARAM_TRANSFORM = "g_transform";
    protected const string PARAM_OPACITY = "g_opacity";

    protected const string PARAM_TEXTURE = "g_texture";
    protected const string PARAM_ALPHATEX = "g_alphatex";
    protected const string PARAM_TEXTURE_VIEWPORT = "g_textureviewport";
    protected const string PARAM_RELATIVE_TRANSFORM = "g_relativetransform";
    protected const string PARAM_BRUSH_TRANSFORM = "g_brushtransform";

    protected const string PARAM_U_OFFSET = "g_uoffset";
    protected const string PARAM_V_OFFSET = "g_voffset";
    protected const string PARAM_TILE_U = "g_tileu";
    protected const string PARAM_TILE_V = "g_tilev";

    #endregion

    #region Protected fields

    protected AbstractProperty _alignmentXProperty;
    protected AbstractProperty _alignmentYProperty;
    protected AbstractProperty _stretchProperty;
    protected AbstractProperty _viewPortProperty;
    protected AbstractProperty _tileModeProperty;

    protected bool _refresh = true;
    protected bool _simplemode = false;
    protected EffectAsset _effect;
    protected Vector4 _textureViewport;
    protected Vector4 _brushTransform;
    #endregion

    #region Ctor

    protected TileBrush()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    void Init()
    {
      _alignmentXProperty = new SProperty(typeof(AlignmentX), AlignmentX.Center);
      _alignmentYProperty = new SProperty(typeof(AlignmentY), AlignmentY.Center);
      _stretchProperty = new SProperty(typeof(Stretch), Stretch.Fill);
      _tileModeProperty = new SProperty(typeof(TileMode), TileMode.None);
      _viewPortProperty = new SProperty(typeof(Vector4), new Vector4(0, 0, 1, 1));
    }

    void Attach()
    {
      _alignmentXProperty.Attach(OnPropertyChanged);
      _alignmentYProperty.Attach(OnPropertyChanged);
      _stretchProperty.Attach(OnPropertyChanged);
      _tileModeProperty.Attach(OnPropertyChanged);
      _viewPortProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _alignmentXProperty.Detach(OnPropertyChanged);
      _alignmentYProperty.Detach(OnPropertyChanged);
      _stretchProperty.Detach(OnPropertyChanged);
      _tileModeProperty.Detach(OnPropertyChanged);
      _viewPortProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      TileBrush b = (TileBrush) source;
      AlignmentX = b.AlignmentX;
      AlignmentY = b.AlignmentY;
      Stretch = b.Stretch;
      Tile = b.Tile;
      ViewPort = copyManager.GetCopy(b.ViewPort);
      Attach();
      _refresh = true;
    }

    #endregion

    #region Public properties

    public AbstractProperty AlignmentXProperty
    {
      get { return _alignmentXProperty; }
    }

    /// <summary>
    /// Gets or sets the horizontal alignment of the image within the viewport.
    /// </summary>
    public AlignmentX AlignmentX
    {
      get { return (AlignmentX) _alignmentXProperty.GetValue(); }
      set { _alignmentXProperty.SetValue(value); }
    }

    public AbstractProperty AlignmentYProperty
    {
      get { return _alignmentYProperty; }
    }

    /// <summary>
    /// Gets or sets the vertical alignment of the image within the viewport.
    /// </summary>
    public AlignmentY AlignmentY
    {
      get { return (AlignmentY) _alignmentYProperty.GetValue(); }
      set { _alignmentYProperty.SetValue(value); }
    }

    public AbstractProperty StretchProperty
    {
      get { return _stretchProperty; }
    }

    /// <summary>
    /// Gets or sets the <see cref="Stretch"/> scaling used to fit the image within the viewport.
    /// </summary>
    public Stretch Stretch
    {
      get { return (Stretch) _stretchProperty.GetValue(); }
      set { _stretchProperty.SetValue(value); }
    }

    public AbstractProperty ViewPortProperty
    {
      get { return _viewPortProperty; }
    }

    /// <summary>
    /// Gets or sets the viewport sub-rect that the image is displayed in. Coordinates are in the 0-1 range.
    /// </summary>
    public Vector4 ViewPort
    {
      get { return (Vector4) _viewPortProperty.GetValue(); }
      set { _viewPortProperty.SetValue(value); }
    }

    public AbstractProperty TileProperty
    {
      get { return _tileModeProperty; }
    }

    /// <summary>
    /// Gets or sets the <see cref="TileMode"/> used when the <see cref="ViewPort"/> is smaller than the drawing target.
    /// </summary>
    public TileMode Tile
    {
      get { return (TileMode) _tileModeProperty.GetValue(); }
      set { _tileModeProperty.SetValue(value); }
    }

    #endregion

    /// <summary>
    /// Gets the actual limits of the image within it's texture.
    /// </summary>
    protected virtual Vector2 TextureMaxUV
    {
      get { return new Vector2(1.0f, 1.0f); }
    }

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      _refresh = true;
      FireChanged();
    }

    public override bool BeginRenderBrush(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    {
      if (Texture == null)
        return false;

      Matrix finalTransform = renderContext.Transform.Clone();

      if (_refresh) 
      {
        RefreshEffectParameters();
        _effect = ServiceRegistration.Get<ContentManager>().GetEffect(_simplemode ? EFFECT_TILE_SIMPLE : EFFECT_TILE);
        _refresh = false;
      }

      if (_simplemode)
        SetSimpleEffectParameters(renderContext);   
      else
        SetEffectParameters(renderContext);  

      _effect.StartRender(Texture, finalTransform);

      return true;
    }

    public override void BeginRenderOpacityBrush(Texture tex, RenderContext renderContext)
    {
      if (Texture == null)
        return;

      Matrix finalTransform = renderContext.Transform.Clone();

      if (_refresh)
      {
        RefreshEffectParameters();
        _effect = ServiceRegistration.Get<ContentManager>().GetEffect(_simplemode ? EFFECT_TILE_OPACITY_SIMPLE : EFFECT_TILE_OPACITY);
        _refresh = false;
      }

      if (_simplemode)
        SetSimpleEffectParameters(renderContext);
      else
        SetEffectParameters(renderContext);   

      _effect.Parameters[PARAM_ALPHATEX] = Texture;
      _effect.StartRender(tex, finalTransform);

      return;
    }

    public override void EndRender()
    {
      if (_effect != null)
        _effect.EndRender();
    }

    protected void RefreshEffectParameters()
    {
      float w = _vertsBounds.Width;
      float h = _vertsBounds.Height;
      Vector2 maxuv = TextureMaxUV;

      Vector4 brushRect = ViewPort;
      // Determine image rect in viewport space
      if (Stretch != Stretch.Fill)
      {
        // Convert brush dimensions to viewport space
        Vector2 brushSize = BrushDimensions;
        brushSize.X /= w * maxuv.X;
        brushSize.Y /= h * maxuv.Y;
        switch (Stretch)
        {
          case Stretch.None:
            // Center (or alignment), original size
            break;

          case Stretch.Uniform:
            // Center (or alignment), keep aspect ratio and show borders
            {
              float ratio = System.Math.Min(ViewPort.Z / brushSize.X, ViewPort.W / brushSize.Y);
              brushSize.X *= ratio;
              brushSize.Y *= ratio;
            }
            break;

          case Stretch.UniformToFill:
            // Center (or alignment), keep aspect ratio, zoom in to avoid borders
            {
              float ratio = System.Math.Max(ViewPort.Z / brushSize.X, ViewPort.W / brushSize.Y);
              brushSize.X *= ratio;
              brushSize.Y *= ratio;
            }
            break;
        }
        // Align brush in viewport
        brushRect = AlignBrushInViewport(brushSize);
      }
      else
      {
        // Compensate for any texture borders
        brushRect.Z /= maxuv.X;
        brushRect.W /= maxuv.Y;
      }

      float repeatx = 1.0f / brushRect.Z;
      float repeaty = 1.0f / brushRect.W;

      // Transform ViewPort into Texture-space and store for later use in tiling
      _textureViewport = new Vector4
        {
            X = ViewPort.X*repeatx - brushRect.X*repeatx,
            Y = ViewPort.Y*repeaty - brushRect.Y*repeaty,
            Z = ViewPort.Z*repeatx,
            W = ViewPort.W*repeaty
        };

      // This structure is used for modifying vertex texture coords to position the brush texture
      _brushTransform = new Vector4(brushRect.X * repeatx, brushRect.Y * repeaty, repeatx, repeaty);

      // Determine if we can use the simpler, more optimised effects
      if (Tile == TileMode.None && Stretch != Stretch.UniformToFill)
        _simplemode = true;
      else if (ViewPort.X <= 0.0f && ViewPort.Z >= 1.0f && ViewPort.Y <= 0.0f && ViewPort.W >= 1.0f)
        _simplemode = true;
      else
        _simplemode = false;
    }

    protected void SetEffectParameters(RenderContext renderContext)
    {
      Vector2 uvoffset = new Vector2(0.0f, 0.0f);
      switch (Tile)
      {
        case TileMode.Tile:
          // Tile both directions
          _effect.Parameters[PARAM_TILE_U] = 1; // D3DTADDRESS_WRAP
          _effect.Parameters[PARAM_TILE_V] = 1; // D3DTADDRESS_WRAP
          break;
        case TileMode.FlipX:
          // Tile both directions but mirror texture on alterate repeats in u/x direction
          _effect.Parameters[PARAM_TILE_U] = 2; // D3DTADDRESS_MIRROR
          _effect.Parameters[PARAM_TILE_V] = 1; // D3DTADDRESS_WRAP
          uvoffset.X = 1.0f - TextureMaxUV.X;
          break;
        case TileMode.FlipY:
          // Tile both directions but mirror texture on alterate repeats in v/y direction
          _effect.Parameters[PARAM_TILE_U] = 1; // D3DTADDRESS_WRAP
          _effect.Parameters[PARAM_TILE_V] = 2; // D3DTADDRESS_MIRROR
          uvoffset.Y = 1.0f - TextureMaxUV.Y;
          break;
        case TileMode.FlipXY:
          // Tile and mirror texture in both directions
          _effect.Parameters[PARAM_TILE_U] = 2; // D3DTADDRESS_MIRROR
          _effect.Parameters[PARAM_TILE_V] = 2; // D3DTADDRESS_MIRROR
          uvoffset = TextureMaxUV;
          uvoffset.X = 1.0f - uvoffset.X;
          uvoffset.Y = 1.0f - uvoffset.Y;
          break;
        case TileMode.None:
        default:
          // No tiling
          _effect.Parameters[PARAM_TILE_U] = 4; // D3DTADDRESS_BORDER
          _effect.Parameters[PARAM_TILE_V] = 4; // D3DTADDRESS_BORDER
          break;
      }

      if (RelativeTransform != null)
        _effect.Parameters[PARAM_RELATIVE_TRANSFORM] = Matrix.Invert(RelativeTransform.GetTransform());
      else
        _effect.Parameters[PARAM_RELATIVE_TRANSFORM] = Matrix.Identity;

      _effect.Parameters[PARAM_TRANSFORM] = GetCachedFinalBrushTransform();
      _effect.Parameters[PARAM_OPACITY] = (float)(Opacity * renderContext.Opacity);
      _effect.Parameters[PARAM_TEXTURE_VIEWPORT] = _textureViewport;
      _effect.Parameters[PARAM_BRUSH_TRANSFORM] = _brushTransform;
      _effect.Parameters[PARAM_U_OFFSET] = uvoffset.X;
      _effect.Parameters[PARAM_V_OFFSET] = uvoffset.Y;
    }

    protected void SetSimpleEffectParameters(RenderContext renderContext)
    {
      if (RelativeTransform != null)
        _effect.Parameters[PARAM_RELATIVE_TRANSFORM] = Matrix.Invert(RelativeTransform.GetTransform());
      else
        _effect.Parameters[PARAM_RELATIVE_TRANSFORM] = Matrix.Identity;

      _effect.Parameters[PARAM_TRANSFORM] = GetCachedFinalBrushTransform();
      _effect.Parameters[PARAM_OPACITY] = (float)(Opacity * renderContext.Opacity);
      _effect.Parameters[PARAM_TEXTURE_VIEWPORT] = _textureViewport;
      _effect.Parameters[PARAM_BRUSH_TRANSFORM] = _brushTransform;
    }

    protected virtual Vector4 AlignBrushInViewport(Vector2 brush_size)
    {
      Vector4 rect = new Vector4();
      switch (AlignmentX)
      {
        case AlignmentX.Left:
          rect.X = ViewPort.X;
          break;

        case AlignmentX.Right:
          rect.X = ViewPort.X + ViewPort.Z - brush_size.X;
          break;

        case AlignmentX.Center:
        default:
          rect.X = ViewPort.X + (ViewPort.Z - brush_size.X) / 2;
          break;
      }
      switch (AlignmentY)
      {
        case AlignmentY.Top:
          rect.Y = ViewPort.Y;
          break;

        case AlignmentY.Bottom:
          rect.Y = ViewPort.Y + ViewPort.W - brush_size.Y;
          break;

        case AlignmentY.Center:
        default:
          rect.Y = ViewPort.Y + (ViewPort.W - brush_size.Y) / 2;
          break;
      }
      rect.Z = brush_size.X;
      rect.W = brush_size.Y;
      return rect;
    }

    protected virtual Vector2 BrushDimensions
    {
      get { return new Vector2(1, 1); }
    }
  }
}
