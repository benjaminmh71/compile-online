extends Node

var port = 8910
var address = "157.245.123.33"
@export var text = 0

func _ready():
	get_node("VBoxContainer").get_node("Label").text = str(text)
	multiplayer.connected_to_server.connect(connected_to_server)

	if ("--server" in OS.get_cmdline_args()):
		var peer = WebSocketMultiplayerPeer.new()
		var error = peer.create_server(port)
		if (error != OK):
			print("Error: " + str(error))

		multiplayer.multiplayer_peer = peer
		print("Start")
	else:
		print("Start")
		var peer = WebSocketMultiplayerPeer.new()
		peer.create_client("ws://" + address + ":" + str(port))
		
		multiplayer.multiplayer_peer = peer

func connected_to_server():
	print("Connected")

func _on_button_pressed():
	update_text.rpc()

@rpc("any_peer", "call_local")
func update_text():
	text += 1
	get_node("VBoxContainer").get_node("Label").text = str(text)
