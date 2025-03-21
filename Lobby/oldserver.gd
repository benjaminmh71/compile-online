#extends Node
#
#var peer := WebSocketMultiplayerPeer.new()
#
#@onready var lobby: Lobby = get_parent()
#@onready var port := lobby.port
#
#func init() -> Server:
	#var error = peer.create_server(port)
	#if (error != OK):
		#print("Error: " + str(error))
	#
	#peer.connect("peer_connected", peer_connected)
	#
	#print("Started Server")
	#
	#return self
#
#func process():
	#peer.poll()
	#if (peer.get_available_packet_count() > 0):
		#var packet := peer.get_packet()
		#if (packet != null):
			#var data = JSON.parse_string(packet.get_string_from_utf8())
			#if (data.message == lobby.Message.BUTTON):
				#var message = {
					#"message" : lobby.Message.BUTTON
				#}
				#peer.put_packet(JSON.stringify(message).to_utf8_buffer())
#
#func peer_connected(id):
	#print("Peer connected")
