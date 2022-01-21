# frozen_string_literal: true

Rails.application.routes.draw do
  use_doorkeeper_openid_connect
  use_doorkeeper
  devise_for :users, controllers: { omniauth_callbacks: 'omniauth_callbacks' }

  # devise_scope :user do
  #   get 'auth/auth0/callback' => 'omniauth_callbacks#auth0'
  #   get 'auth/auth0', to: '/devise/omniauth_callbacks#passthru', as: :omniauth_authorize, via: %i[get post]
  # end

  # For details on the DSL available within this file, see https://guides.rubyonrails.org/routing.html
  root to: 'home#index'
end
