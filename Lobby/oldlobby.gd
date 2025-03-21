#extends Node
#
#var port := 8910
#var address := "157.245.123.33"
#@export var text := 0
#var connectionManager
#enum Message {
	#BUTTON,
	#CREATE_ROOM
#}
#
#func _ready():
	#get_node("VBoxContainer").get_node("Label").text = str(text)
	#
	#if ("--server" in OS.get_cmdline_args()):
		#connectionManager = $Server.init()
	#else:
		#connectionManager = $Client.init()
#
#func _process(delta):
	#connectionManager.process()
	#get_node("VBoxContainer").get_node("Label").text = str(text)
