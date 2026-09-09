local spr = app.sprite
local function layerByName(name)
  for _, l in ipairs(spr.layers) do
    if l.name == name then return l end
  end
end

app.transaction(function()
  layerByName("Body Gear").isVisible = false
  layerByName("Head Gear").isVisible = false
  spr:saveAs(spr.filename)
end)
print("DONE fixing visibility")
