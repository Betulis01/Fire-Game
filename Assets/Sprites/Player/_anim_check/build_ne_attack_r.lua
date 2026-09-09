local spr = app.sprite
local W, H = spr.width, spr.height

local function layerByName(name)
  for _, l in ipairs(spr.layers) do
    if l.name == name then return l end
  end
  return nil
end

local function fullCanvasImage()
  return Image(W, H)
end

-- Read a pixel in canvas space from a cel (handles cel offset).
local function getCanvasPixel(cel, x, y)
  if not cel then return Color(0,0,0,0) end
  local img = cel.image
  local lx, ly = x - cel.position.x, y - cel.position.y
  if lx < 0 or ly < 0 or lx >= img.width or ly >= img.height then
    return Color(0,0,0,0)
  end
  return Color(img:getPixel(lx, ly))
end

app.transaction(function()
  local boxL, bodyL, headL, bgearL, hgearL, weaponL =
    layerByName("Box"), layerByName("Body"), layerByName("Head"),
    layerByName("Body Gear"), layerByName("Head Gear"), layerByName("Weapon")

  -- source frames (0-based) 46,47,48 -> dest 49,50,51
  for i = 0, 2 do
    local srcF, dstF = 46 + i, 49 + i
    for _, layer in ipairs({boxL, bodyL, headL, bgearL, hgearL, weaponL}) do
      local srcCel = layer:cel(srcF + 1)
      local dstImg = fullCanvasImage()
      if srcCel then
        for y = 0, H - 1 do
          for x = 0, W - 1 do
            local c = getCanvasPixel(srcCel, x, y)
            if c.alpha > 0 then
              local destX = x
              if layer ~= boxL then
                destX = (W - 1) - x -- mirror character layers only, keep ground tile as-is
              end
              dstImg:drawPixel(destX, y, c)
            end
          end
        end
      end
      spr:newCel(layer, dstF + 1, dstImg, Point(0, 0))
    end
  end

  -- Tag the new animation
  local tag = spr:newTag(50, 52) -- 1-based frame numbers = 0-based 49..51
  tag.name = "ne_attack_r"
end)

spr:saveAs(spr.filename)
print("DONE building ne_attack_r, saved")
