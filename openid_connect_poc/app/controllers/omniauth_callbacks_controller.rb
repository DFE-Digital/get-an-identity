class OmniauthCallbacksController < Devise::OmniauthCallbacksController
  def auth0
    @user = User.from_omniauth(request.env['omniauth.auth'])
    sign_in @user, event: :authentication
    redirect_to session[:return_to] || root_path
  end

  def gds
    puts "Auth hash #{request.env['omniauth.auth']}"
    @user = User.from_omniauth(request.env['omniauth.auth'])
    sign_in @user, event: :authentication
    redirect_to session[:return_to] || root_path
  end

  def failure
    render json: { error: request.env['omniauth.error'], type: request.env['omniauth.error.type'] }
  end
end
