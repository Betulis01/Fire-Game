local spr = app.sprite
local W, H = spr.width, spr.height

local function layerByName(name)
  for _, l in ipairs(spr.layers) do
    if l.name == name then return l end
  end
end

local bodyL = layerByName("Body")
local weaponL = layerByName("Weapon")

local function getCanvasPixel(cel, x, y)
  if not cel then return Color(0,0,0,0) end
  local img = cel.image
  local lx, ly = x - cel.position.x, y - cel.position.y
  if lx < 0 or ly < 0 or lx >= img.width or ly >= img.height then
    return Color(0,0,0,0)
  end
  return Color(img:getPixel(lx, ly))
end

local function findHand(f)
  local cel = bodyL:cel(f + 1)
  local ax, ay = 32, 39
  local bestX, bestY, bestD = nil, nil, -1
  for y = 36, 44 do
    for x = 15, 50 do
      -- exclude leg pixels: legs are the low, near-centerline columns
      local isLegLike = (y >= 43) and (math.abs(x - 32) < 8)
      if not isLegLike then
        local c = getCanvasPixel(cel, x, y)
        if c.alpha > 0 then
          local d = (x-ax)*(x-ax) + (y-ay)*(y-ay)
          if d > bestD then bestD = d; bestX = x; bestY = y end
        end
      end
    end
  end
  return bestX, bestY
end

local HILT = Color{r=120, g=76, b=40, a=255}
local BLADE = Color{r=214, g=217, b=225, a=255}
local BLADE_EDGE = Color{r=170, g=176, b=188, a=255}

app.transaction(function()
  weaponL.blendMode = BlendMode.NORMAL
  weaponL.opacity = 255

  local ax, ay = 32, 39
  for f = 40, 51 do
    local hx, hy = findHand(f)
    local dx, dy = hx - ax, hy - ay
    local dist = math.sqrt(dx*dx + dy*dy)
    dx, dy = dx / dist, dy / dist

    local img = Image(W, H)
    -- step 0 = hilt at hand, steps 1..4 = blade extending outward
    local placed = {}
    for t = 0, 4 do
      local px = math.floor(hx + dx * t + 0.5)
      local py = math.floor(hy + dy * t + 0.5)
      local key = px .. "," .. py
      if not placed[key] then
        placed[key] = true
        local col
        if t == 0 then
          col = HILT
        elseif t == 4 then
          col = BLADE_EDGE
        else
          col = BLADE
        end
        if px >= 0 and py >= 0 and px < W and py < H then
          img:drawPixel(px, py, col)
        end
      end
    end
    spr:newCel(weaponL, f + 1, img, Point(0, 0))
  end

  spr:saveAs(spr.filename)
end)

print("DONE drawing weapons")
