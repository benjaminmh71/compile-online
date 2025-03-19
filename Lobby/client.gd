extends Node
class_name Client

var peer := WebSocketMultiplayerPeer.new()

@onready var lobby: Lobby = get_parent()
@onready var address := lobby.address
@onready var port := lobby.port

func init() -> Client:
	var error = peer.create_client("ws://" + address + ":" + str(port))
	if (error != OK):
		print("Error: " + str(error))
	
	peer.connect("peer_connected", peer_connected)
	
	print("Started Client")
	
	return self

func process():
	peer.poll()
	if (peer.get_available_packet_count() > 0):
		var packet := peer.get_packet()
		if (packet != null):
			var data = JSON.parse_string(packet.get_string_from_utf8())
			if (data.message == lobby.Message.BUTTON):
				lobby.text += 1

func peer_connected(id):
	print("Peer connected")

func _on_button_pressed() -> void:
	var message = {
		"message": lobby.Message.BUTTON
	}
	peer.put_packet(JSON.stringify(message).to_utf8_buffer())
