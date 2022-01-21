# frozen_string_literal: true

Doorkeeper::OpenidConnect.configure do
  issuer do |_resource_owner, _application|
    'http://localhost:3000/'
  end

  signing_key <<~KEY
    -----BEGIN PRIVATE KEY-----
    MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDG+SFYhORiprbf
    oji56qNjztaX8cGOrnF4meEhP7pahVnKwA8peIZhevT8VkxLB7ntjW/MSpqfGKT9
    p5RM7xy0ATojux0dGOTi0yQRYcYl4Xdvrw8ow3iSNhtmBel9YalAwTzlaZpS+g7Q
    0SR195PV9kqfSFCK2fX3QstbYOQhGbpUvDvtrbmgiLQRm25UmPxGliaE370yN9UA
    ULuMgpzC//xzG43ayLA75637pkXrNjAVTuut5N5wMxyDCEBuSCfleT8bSPiWMQUK
    Sp3Ra7w5+Xb2puI99RRD0g9c5izdStW8NrNaPdROCJC7tS8/ckFyxSewFijfzi6f
    +GAz9tIxAgMBAAECggEBAKab48U4efj1OnomXzOmeyxO/Tf8EqSEA7YFvBLYRxYX
    RWnVypxiNLwZdlx5cqb9ED4PSccZzUFdJILVuQN20WUVBfb3blh4COi5/iCj64S6
    uAUH5Dyw+RwNPIIAf1Qi29PCO1iqRbQneRJ3nf0900e9VRztM6wg+KoT/y5EAqKH
    a5sNHV/RoFM+C2CNbrwo6Kfiddxh6sxwAGApyKmqEyCL+34dRTim110E322U4UXM
    0DFzKjcpO3RmyOxseoeE+8FZ6Av7kr+nIqYZrNcflWQAZCJX/KdX4APY5VcgWQl9
    Pk+JncZw6LBocuUW4xUx7dYKip0RbipHkjPCfphdiRUCgYEA4obPfqYchGlF/pfE
    LvndOuhpyI410rM/LyJKrjRj/+ep1yvfqKjj2ljDvrSz9C2vUrA89EDsJ0sReWNa
    dRAVhuSVh+X5ow9kEHjppBlkEAQynBNw9Jzt6WQfQIZrYjpfgUlAcIA0sE87Ccoh
    hI8BnJ4v9jfx19qSt2YKJmNovQcCgYEA4NyQlKvWFLhhexmsVT/DHhcFw/peZzU1
    pxfpHbdY1SadX/7yUoYQVOkdd4jLw5E6UARJ89UFlBuso1ZdUuGDttt6XCTLAQAy
    p7YWBNTv1kC2ZqktggoBliRBYf/FVRzEZZueGdZCMCHSetvd9dw8GiGkTNAEDpx1
    RswwOWs2YQcCgYA1AUR2JxpPJW76ZrrCHzdT/GQcSKJxff3P4p9E6f9oNuX38k0w
    YuyF//U1n4ToIvR+TbzFjpdzjk41cDkPYUcYPE588SQbspNAg3pwKnzOfpz1BluM
    8Vd+IC5r48gmwO/uCZzpdiZeBvwSi1iScv/2jNE+NNMDJiLkhRzk5KfyawKBgQCy
    KVBM25G1vRlPhdnreafJMYiZ7Mfbkmc+S02jA+BYkk3i/4dUJ3DNNh7o1PRNscW4
    HI3TqhbPcNXqXMV4o8HOojtiwqwt0NBR3Y24qlaVNZTP5n9uJyt2oKdFVHgvpale
    sFwmMIMky8ePHKHS6XqdYcZiLfbo9MJfI+2ZsP7XBQKBgCoPHvig8FVVg7J02Wfd
    VV4uuX6NR5UTASYHjVB5TPkiDassj/HfghvViZAetFTDANpj0twJ4f2mxYA+kpi9
    rPfbNBlg8AfKYzNX8bQibvEq8Pqa8zsXInbW8NZ45rzoXnmKGo7/+dUflP/2p3Sw
    A2QXGCvCZzhnP1wnuO9KaMj8
    -----END PRIVATE KEY-----
  KEY

  subject_types_supported [:public]

  resource_owner_from_access_token do |access_token|
    # Example implementation:
    User.find_by(id: access_token.resource_owner_id)
  end

  auth_time_from_resource_owner do |resource_owner|
    # Example implementation:
    # resource_owner.current_sign_in_at
  end

  reauthenticate_resource_owner do |resource_owner, return_to|
    # Example implementation:
    # store_location_for resource_owner, return_to
    # sign_out resource_owner
    # redirect_to new_user_session_url
  end

  # Depending on your configuration, a DoubleRenderError could be raised
  # if render/redirect_to is called at some point before this callback is executed.
  # To avoid the DoubleRenderError, you could add these two lines at the beginning
  #  of this callback: (Reference: https://github.com/rails/rails/issues/25106)
  #   self.response_body = nil
  #   @_response_body = nil
  select_account_for_resource_owner do |resource_owner, return_to|
    # Example implementation:
    # store_location_for resource_owner, return_to
    # redirect_to account_select_url
  end

  subject do |resource_owner, _application|
    # Example implementation:
    resource_owner.id

    # or if you need pairwise subject identifier, implement like below:
    # Digest::SHA256.hexdigest("#{resource_owner.id}#{URI.parse(application.redirect_uri).host}#{'your_secret_salt'}")
  end

  # Protocol to use when generating URIs for the discovery endpoint,
  # for example if you also use HTTPS in development
  # protocol do
  #   :https
  # end

  # Expiration time on or after which the ID Token MUST NOT be accepted for processing. (default 120 seconds).
  # expiration 600

  # Example claims:
  # claims do
  #   normal_claim :_foo_ do |resource_owner|
  #     resource_owner.foo
  #   end

  #   normal_claim :_bar_ do |resource_owner|
  #     resource_owner.bar
  #   end
  # end
  claims do
    normal_claim :trn do |resource_owner|
      resource_owner.trn
    end
  end
end
