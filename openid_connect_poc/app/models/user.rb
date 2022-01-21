# frozen_string_literal: true

class User < ApplicationRecord
  # Include default devise modules. Others available are:
  # :confirmable, :lockable, :timeoutable, :trackable and :omniauthable
  devise :database_authenticatable, :rememberable, :trackable, :omniauthable

  has_many :access_grants,
           class_name: 'Doorkeeper::AccessGrant',
           dependent: :delete_all,
           foreign_key: :resource_owner_id

  has_many :access_tokens,
           class_name: 'Doorkeeper::AccessToken',
           dependent: :delete_all,
           foreign_key: :resource_owner_id

  validates :email, presence: true, uniqueness: true

  def self.from_omniauth(auth)
    where(provider: auth.provider, uid: auth.uid).first_or_create do |user|
      user.email = auth.info.email
      user.password = user.password_confirmation = Devise.friendly_token[0, 20]
    end
  end

  def trn
    SecureRandom.hex(10)
  end
end
