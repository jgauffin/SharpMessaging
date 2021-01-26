Simple protocol
================

This is our own network protocol used for these streams.

It's based on a binary structure.

# Handshake

Sent when the connection is established.

Contains one byte today which corresponds to the version number

1

# Messages

These messages are supported in version 1.0.

Each message contains a byte prefix which corresponds to the message typ

1. Message
2. ACK
3. NAK

## Message

The endpoint want to publish a message.

Each message starts with headers looped.

Repeated until featureflag contains 128
  FeatureFlag
  Name
  Value
CONTENT

### FeatureFlag

Feature flag tells what kind of value the header name or value contains.

0: String header
1: Indexed header (all string headers should be indexed by the receiving endpoint to be reused later in the session)
128: Last header (once the header have been parsed successfully, the content will start. Use the "Content-Length" header to know how large the header is.)